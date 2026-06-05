import json
import logging
import threading
import time
from datetime import datetime, timezone
from typing import Optional, List
import pika
from pydantic import ValidationError

from app.models.events import (
    ImageProcessingCompletedEvent,
    ImageProcessingFailedEvent,
    ImageUploadedEvent,
)
from app.models.schemas import JobStatus
from app.services.image_processor import ImageProcessingService
from app.services.database_service import DatabaseService
from app.services.guardrails import ContentModerationError
from app.infrastructure.database import get_db

logger = logging.getLogger(__name__)

# Type alias for JSON message data (simplified to avoid recursion)
MessageData = dict[str, object]


class RabbitMqConnection:
    """Manages RabbitMQ connection lifecycle."""
    
    def __init__(self, host: str, username: str, password: str):
        self._host = host
        self._username = username
        self._password = password
        self._connection: Optional[pika.BlockingConnection] = None
        self._channel: Optional[pika.channel.Channel] = None
    
    def connect(self) -> pika.channel.Channel:
        """Establish connection and return channel."""
        credentials = pika.PlainCredentials(self._username, self._password)
        parameters = pika.ConnectionParameters(
            host=self._host,
            credentials=credentials,
            heartbeat=600,
            blocked_connection_timeout=300,
        )
        self._connection = pika.BlockingConnection(parameters)
        self._channel = self._connection.channel()
        logger.info("RabbitMQ connection established")
        return self._channel
    
    def close(self) -> None:
        """Close connection and channel."""
        if self._channel and self._channel.is_open:
            self._channel.close()
        if self._connection and self._connection.is_open:
            self._connection.close()
        logger.info("RabbitMQ connection closed")
    
    @property
    def channel(self) -> Optional[pika.channel.Channel]:
        return self._channel


class RabbitMqPublisher:
    """Publishes messages to RabbitMQ exchanges."""
    
    def publish(
        self, 
        exchange: str, 
        message: MessageData, 
        channel: pika.channel.Channel,
        exchange_type: str = "fanout"
    ) -> None:
        """Publish a message to the specified exchange."""
        # Declare exchange
        channel.exchange_declare(exchange=exchange, exchange_type=exchange_type, durable=True)
        
        body = json.dumps(message, default=str).encode("utf-8")
        
        channel.basic_publish(
            exchange=exchange,
            routing_key="",
            body=body,
            properties=pika.BasicProperties(
                delivery_mode=2,  # Persistent
                content_type="application/json",
            ),
        )
        logger.info("Published message to exchange '%s'", exchange)


class RabbitMqConsumer:
    """Consumes messages from RabbitMQ and processes images."""
    
    QUEUE_NAME = "ia-processing-queue"
    INPUT_EXCHANGE = "image-events"
    OUTPUT_EXCHANGE = "processed-image-events"
    
    def __init__(self, connection: RabbitMqConnection):
        self._connection = connection
        self._publisher = RabbitMqPublisher()
        self._processor = ImageProcessingService()
        self._thread: Optional[threading.Thread] = None
    
    def start(self) -> None:
        """Start consumer in background thread."""
        self._thread = threading.Thread(target=self._consume, daemon=True)
        self._thread.start()
        logger.info("RabbitMQ Consumer started on queue '%s'", self.QUEUE_NAME)
    
    def _consume(self) -> None:
        """Main consumption loop with retry logic."""
        max_retries = 10
        retry_delay = 5  # seconds
        
        for attempt in range(max_retries):
            try:
                channel = self._connection.connect()
                
                # Declare input exchange and bind queue
                channel.exchange_declare(exchange=self.INPUT_EXCHANGE, exchange_type="fanout", durable=True)
                channel.queue_declare(queue=self.QUEUE_NAME, durable=True)
                channel.queue_bind(queue=self.QUEUE_NAME, exchange=self.INPUT_EXCHANGE, routing_key="")
                
                channel.basic_qos(prefetch_count=1)
                channel.basic_consume(
                    queue=self.QUEUE_NAME,
                    on_message_callback=self._on_message,
                    auto_ack=False,
                )
                
                logger.info("RabbitMQ consumer ready - waiting for messages")
                channel.start_consuming()
                return  # Exit on successful connection
                
            except pika.exceptions.AMQPConnectionError as e:
                if attempt < max_retries - 1:
                    logger.warning(
                        f"RabbitMQ connection failed (attempt {attempt + 1}/{max_retries}), "
                        f"retrying in {retry_delay}s..."
                    )
                    time.sleep(retry_delay)
                    retry_delay = min(retry_delay * 2, 60)  # Exponential backoff, max 60s
                else:
                    logger.exception("RabbitMQ consumer failed after max retries")
                    raise
            except Exception:
                logger.exception("RabbitMQ consumer error")
                raise
    
    def _on_message(
        self,
        channel: pika.channel.Channel,
        method: pika.spec.Basic.Deliver,
        properties: pika.BasicProperties,
        body: bytes,
    ) -> None:
        """Handle incoming message."""
        try:
            raw = body.decode("utf-8")
            logger.info("Received message: %s", raw)
            
            data: MessageData = json.loads(raw)
            
            # Skip if already processed
            if "OwnerId" not in data or "ProcessedImageUrls" in data or "ErrorMessage" in data:
                logger.debug("Skipping non-upload event")
                channel.basic_ack(delivery_tag=method.delivery_tag)
                return
            
            try:
                event = ImageUploadedEvent.model_validate(data)
            except ValidationError as ve:
                logger.warning("Invalid ImageUploadedEvent: %s", ve)
                channel.basic_ack(delivery_tag=method.delivery_tag)
                return
            
            # Process with database tracking
            self._process_event(event, channel, method)
            
        except Exception:
            logger.exception("Error handling message")
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=False)
    
    def _process_event(
        self,
        event: ImageUploadedEvent,
        channel: pika.channel.Channel,
        method: pika.spec.Basic.Deliver
    ) -> None:
        """Process image event with database tracking."""
        db = get_db()
        db_service = DatabaseService(db)
        job = None
        
        try:
            # Create job record
            job = db_service.create_job(
                original_image_id=event.ImageId,
                user_id=event.OwnerId,
                original_image_url=event.ImageUrl,
                prompt=event.Prompt,
                parameters=event.Parameters.model_dump() if event.Parameters else {}
            )
            
            # Update status to processing
            db_service.update_job_status(job.id, JobStatus.PROCESSING)
            
            # Process the image
            processed_urls, results = self._processor.process(event)
            
            # Update job with results
            db_service.update_job_status(
                job_id=job.id,
                status=JobStatus.COMPLETED,
                processed_urls=processed_urls,
                processing_results=results.model_dump(mode="json")
            )
            
            # Publish completion event
            completed = ImageProcessingCompletedEvent(
                ImageId=event.ImageId,
                UserId=event.OwnerId,
                ProjectId=event.ProjectId,
                ImageUrl=event.ImageUrl,
                ProcessedImageUrls=processed_urls,
                ProcessingResults=results,
                CompletedAt=datetime.now(timezone.utc),
            )
            
            self._publisher.publish(
                self.OUTPUT_EXCHANGE,
                completed.model_dump(mode="json"),
                channel
            )
            
            logger.info(
                "Image %s processed successfully, job %s, URLs: %s",
                event.ImageId,
                job.id,
                processed_urls,
            )
            
        except ContentModerationError as mod_ex:
            # Content blocked by guardrails
            error_message = f"Content blocked: {mod_ex.reason}"
            if job:
                db_service.update_job_status(
                    job_id=job.id,
                    status=JobStatus.FAILED,
                    error_message=error_message
                )

            failed = ImageProcessingFailedEvent(
                ImageId=event.ImageId,
                UserId=event.OwnerId,
                ProjectId=event.ProjectId,
                ImageUrl=event.ImageUrl,
                ErrorMessage=error_message,
                ErrorCode="CONTENT_MODERATION_VIOLATION",
                FailedAt=datetime.now(timezone.utc),
                ModelUsed=event.Parameters.model if event.Parameters else None,
            )

            self._publisher.publish(
                self.OUTPUT_EXCHANGE,
                failed.model_dump(mode="json"),
                channel
            )

            logger.warning("Image %s blocked by content moderation: %s", event.ImageId, mod_ex)

        except Exception as proc_ex:
            # Update job with error
            if job:
                db_service.update_job_status(
                    job_id=job.id,
                    status=JobStatus.FAILED,
                    error_message=str(proc_ex)
                )

            # Publish failure event
            failed = ImageProcessingFailedEvent(
                ImageId=event.ImageId,
                UserId=event.OwnerId,
                ProjectId=event.ProjectId,
                ImageUrl=event.ImageUrl,
                ErrorMessage=str(proc_ex),
                ErrorCode="PROCESSING_ERROR",
                FailedAt=datetime.now(timezone.utc),
                ModelUsed=event.Parameters.model if event.Parameters else None,
            )
            
            self._publisher.publish(
                self.OUTPUT_EXCHANGE,
                failed.model_dump(mode="json"),
                channel
            )
            
            logger.error("Image %s processing failed: %s", event.ImageId, proc_ex)
            
        finally:
            db.close()
        
        channel.basic_ack(delivery_tag=method.delivery_tag)
    
    def stop(self) -> None:
        """Stop consumer and close connection."""
        if self._connection.channel and self._connection.channel.is_open:
            self._connection.channel.stop_consuming()
        self._connection.close()
        logger.info("RabbitMQ Consumer stopped")

import json
import logging
import threading
from datetime import datetime, timezone
import pika
from pydantic import ValidationError
from app.events import (
    ImageProcessingCompletedEvent,
    ImageProcessingFailedEvent,
    ImageUploadedEvent,
)
from app.processor import ImageProcessor, build_processor
from app.publisher import RabbitMqResultPublisher

logger = logging.getLogger(__name__)

class RabbitMqImageConsumer:
    QUEUE_NAME = "ia-processing-queue"

    def __init__(self, host: str, username: str, password: str):
        self._host = host
        self._username = username
        self._password = password
        self._processor: ImageProcessor = build_processor()
        self._publisher = RabbitMqResultPublisher()
        self._connection: pika.BlockingConnection | None = None
        self._channel: pika.channel.Channel | None = None
        self._thread: threading.Thread | None = None

    def start(self) -> None:
        self._thread = threading.Thread(target=self._consume, daemon=True)
        self._thread.start()
        logger.info("Image Processing Consumer started listening on queue '%s'", self.QUEUE_NAME)

    def _consume(self) -> None:
        try:
            credentials = pika.PlainCredentials(self._username, self._password)
            parameters = pika.ConnectionParameters(
                host=self._host,
                credentials=credentials,
                heartbeat=600,
                blocked_connection_timeout=300,
            )
            self._connection = pika.BlockingConnection(parameters)
            self._channel = self._connection.channel()

            # Declare exchange
            self._channel.exchange_declare(exchange="image-events", exchange_type="fanout", durable=True)

            # Declare and bind queue for IA
            self._channel.queue_declare(queue="ia-processing-queue", durable=True)
            self._channel.queue_bind(queue="ia-processing-queue", exchange="image-events", routing_key="")

            self._channel.basic_qos(prefetch_count=1)

            self._channel.basic_consume(
                queue="ia-processing-queue",
                on_message_callback=self._on_message,
                auto_ack=False,
            )

            logger.info("RabbitMQ consumer connection succesfull :D ")
            self._channel.start_consuming()

        except Exception:
            logger.exception(" oh :( error in RabbitMQ consumer )")

    def _on_message(
        self,
        channel: pika.channel.Channel,
        method: pika.spec.Basic.Deliver,
        properties: pika.BasicProperties,
        body: bytes,
    ) -> None:
        try:
            raw = body.decode("utf-8")
            logger.info("Received message: %s", raw)

            data = json.loads(raw)

            if "OwnerId" not in data or "ProcessedImageUrl" in data or "ErrorMessage" in data:
                logger.debug("Skipping non-upload event already processed or failed")
                channel.basic_ack(delivery_tag=method.delivery_tag)
                return

            try:
                event = ImageUploadedEvent.model_validate(data)
            except ValidationError as ve:
                logger.warning("Invalid ImageUploadedEvent: %s", ve)
                channel.basic_ack(delivery_tag=method.delivery_tag)
                return

            try:
                processed_url, results = self._processor.process(event.ImageUrl)

                completed = ImageProcessingCompletedEvent(
                    ImageId=event.ImageId,
                    UserId=event.OwnerId,
                    ImageUrl=event.ImageUrl,
                    ProcessedImageUrl=processed_url,
                    ProcessingResults=results,
                    CompletedAt=datetime.now(timezone.utc),
                )

                self._publisher.publish(self.QUEUE_NAME, completed.model_dump(mode="json"), self._channel)

                logger.info(
                    "Image %s processed successfully  result published",
                    event.ImageId,
                )

            except Exception as proc_ex:
                failed = ImageProcessingFailedEvent(
                    ImageId=event.ImageId,
                    UserId=event.OwnerId,
                    ImageUrl=event.ImageUrl,
                    ErrorMessage=str(proc_ex),
                    ErrorCode="PROCESSING_ERROR",
                    FailedAt=datetime.now(timezone.utc),
                )

                self._publisher.publish(self.QUEUE_NAME, failed.model_dump(mode="json"), self._channel)

                logger.error(
                    "Image %s processing failed: %s",
                    event.ImageId,
                    proc_ex,
                )

            channel.basic_ack(delivery_tag=method.delivery_tag)

        except Exception:
            logger.exception("Error handling message")
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=False)

    def stop(self) -> None:
        if self._channel and self._channel.is_open:
            self._channel.stop_consuming()
        if self._connection and self._connection.is_open:
            self._connection.close()
        logger.info("Image Processing Consumer stopped")

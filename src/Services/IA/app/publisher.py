import json
import logging
import pika

logger = logging.getLogger(__name__)

class RabbitMqResultPublisher:
    def publish(self, queue_name: str, message: dict, channel: pika.channel.Channel) -> None:
        # Declare exchange for processed image events
        channel.exchange_declare(exchange="processed-image-events", exchange_type="fanout", durable=True)

        body = json.dumps(message, default=str).encode("utf-8")

        channel.basic_publish(
            exchange="processed-image-events",
            routing_key="",
            body=body,
            properties=pika.BasicProperties(
                delivery_mode=2,
                content_type="application/json",
            ),
        )
        logger.info("Published message to exchange 'processed-image-events'")

import json
import logging
import pika

logger = logging.getLogger(__name__)

class RabbitMqResultPublisher:
    def publish(self, queue_name: str, message: dict, channel: pika.channel.Channel) -> None:
        channel.queue_declare(queue=queue_name, durable=True)

        body = json.dumps(message, default=str).encode("utf-8")

        channel.basic_publish(
            exchange="",
            routing_key=queue_name,
            body=body,
            properties=pika.BasicProperties(
                delivery_mode=2,
                content_type="application/json",
            ),
        )
        logger.info("Published message to queue '%s'", queue_name)


import json
import os
import pika
from dotenv import load_dotenv

dotenv_path: str = os.path.join(os.path.dirname(__file__), "..", "..", "..", ".env")
load_dotenv(dotenv_path)

rabbitmq_host: str = os.getenv("RabbitMQ__Host", "localhost")
rabbitmq_user: str | None = os.getenv("RabbitMQ__Username")
rabbitmq_pass: str | None = os.getenv("RabbitMQ__Password")

if not rabbitmq_user or not rabbitmq_pass:
    raise ValueError("RabbitMQ__Username and RabbitMQ__Password must be configured in the .env file")

creds: pika.PlainCredentials = pika.PlainCredentials(rabbitmq_user, rabbitmq_pass)
conn: pika.BlockingConnection = pika.BlockingConnection(pika.ConnectionParameters(rabbitmq_host, credentials=creds))
ch: pika.adapters.blocking_connection.BlockingChannel = conn.channel()
ch.queue_declare(queue="image-processing-events", durable=True)

msg: dict[str, str] = {
    "ImageId": "b3f9a2e1-7c4d-4e8f-a1b2-9d0e3f5c6a7b",
    "OwnerId": "a1d4e7c2-3f8b-4a9d-b5e6-2c7f1a0d9e3b",
    "ImageUrl": "https://images.pexels.com/photos/36737091/pexels-photo-36737091.jpeg"
}

ch.basic_publish(
    exchange="",
    routing_key="image-processing-events",
    body=json.dumps(msg).encode(),
    properties=pika.BasicProperties(delivery_mode=2, content_type="application/json")
)
conn.close()
print("ImageUploadedEvent published!")
print("ImageId:", msg["ImageId"])
print("OwnerId:", msg["OwnerId"])
print("ImageUrl:", msg["ImageUrl"])

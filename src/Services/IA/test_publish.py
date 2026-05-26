
import json
import os
from datetime import datetime, timezone
from typing import Union, List
import pika
from dotenv import load_dotenv

# Type alias for JSON message data
JsonValue = Union[str, int, float, bool, None, List['JsonValue'], 'JsonDict']
JsonDict = dict[str, JsonValue]

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

# Declare the fanout exchange (matches consumer.py)
ch.exchange_declare(exchange="image-events", exchange_type="fanout", durable=True)

# Build message with new format (Prompt + Parameters)
# Pixazo (flux-schnell) example - Text-to-Image (no source image needed):
#   "ImageUrl": "",  # Ignored for text-to-image
#   "Prompt": "A futuristic car in neon city",
#   "Parameters": {"width": 512, "height": 512, "quantity": 1, "model": "flux-schnell"}
#
# Image-to-Image models (OpenAI/Pollinations) - require ImageUrl:
#   "ImageUrl": "https://example.com/image.jpg",
#   "Prompt": "Add a moon in the sky",
#   "Parameters": {"width": 512, "height": 512, "strength": 0.75, "model": "kontext"}
# === OPCIÓN 1: Generación de imágenes (Text-to-Image) ===
# Feature: 0 = Generator (text-to-image)
# Modelos: "flux-schnell" (Pixazo, más barato ~$0.0012/imagen)
# NO requiere ImageUrl
msgGenerator: JsonDict = {
    "ImageId": "b3f9a2e1-7c4d-4e8f-a1b2-9d0e3f5c6a7b",
    "OwnerId": "a1d4e7c2-3f8b-4a9d-b5e6-2c7f1a0d9e3b",
    # Sin ImageUrl para text-to-image
    "Prompt": "Un pulpo gigante hecho de vitrales flotando sobre una catedral medieval",
    "Feature": 0,  # Generator
    "Parameters": {
        "width": 512,
        "height": 512,
        "quantity": 1,
        "model": "flux-schnell"
    },
    "UploadedAt": datetime.now(timezone.utc).isoformat()
}

# === OPCIÓN 2: Edición de imágenes (Image-to-Image) ===
# Feature: 1 = Editor (image-to-image)
# Modelos: "gpt-image-1-mini-low", "kontext", "gpt-image-1-mini-high"
# REQUIERE ImageUrl
msgEditor: JsonDict = {
    "ImageId": "b3f9a2e1-7c4d-4e8f-a1b2-9d0e3f5c6a7b",
    "OwnerId": "a1d4e7c2-3f8b-4a9d-b5e6-2c7f1a0d9e3b",
    "ImageUrl": "https://res.cloudinary.com/estebancamacho-jalau/image/upload/v1779714739/processed_images/processed_pixazo_1779714738_0.jpg",
    "Prompt": "Añade una cara al pulpo",
    "Feature": 1,  # Editor
    "Parameters": {
        "width": 512,
        "height": 512,
        "strength": 0.75,
        "quantity": 1,
        "num_inference_steps": 20,
        "guidance_scale": 7.5,
        "model": "gpt-image-1-mini-low"
    },
    "UploadedAt": datetime.now(timezone.utc).isoformat()
}

# Publish to exchange (not directly to queue)
ch.basic_publish(
    exchange="image-events",
    routing_key="",
    body=json.dumps(msgEditor, default=str).encode(),
    properties=pika.BasicProperties(delivery_mode=2, content_type="application/json")
)
conn.close()
print("ImageUploadedEvent published to 'image-events' exchange!")
#print(f"ImageId: {msgEditor['ImageId']}")
#print(f"OwnerId: {msgEditor['OwnerId']}")
#print(f"ImageUrl: {msgEditor.get('ImageUrl', 'N/A (text-to-image)')}")
print(f"Prompt: {msgEditor['Prompt']}")
print(f"Feature: {msgEditor['Feature']} (0=Generator, 1=Editor)")
print(f"Model: {msgEditor['Parameters']['model']}")  # type: ignore[index]

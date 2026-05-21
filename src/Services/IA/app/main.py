

import logging
import os
from contextlib import asynccontextmanager

from dotenv import load_dotenv
from fastapi import FastAPI

from app.consumer import RabbitMqImageConsumer


env_path: str = os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".env")
if os.path.exists(env_path):
    load_dotenv(env_path)


logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)

logger: logging.Logger = logging.getLogger(__name__)


RABBITMQ_HOST: str = os.getenv("RabbitMQ__Host", "localhost")
RABBITMQ_USERNAME: str | None = os.getenv("RabbitMQ__Username")
RABBITMQ_PASSWORD: str | None = os.getenv("RabbitMQ__Password")

if not RABBITMQ_USERNAME or not RABBITMQ_PASSWORD:
    raise ValueError("RabbitMQ__Username and RabbitMQ__Password must be configured in the .env file (tomorrow im gonna leak the gta 6 trailer)")


consumer: RabbitMqImageConsumer | None = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    global consumer
    consumer = RabbitMqImageConsumer(
        host=RABBITMQ_HOST,
        username=RABBITMQ_USERNAME,
        password=RABBITMQ_PASSWORD,
    )
    consumer.start()
    logger.info("PixPro IA Service started — MockAI processor ready")

    yield

    if consumer:
        consumer.stop()
    logger.info("PixPro IA Service stopped")


app = FastAPI(
    title="PixPro IA Service",
    description="Image Processing Microservice with MockAI",
    version="1.0.0",
    lifespan=lifespan,
)


@app.get("/health")
async def health():
    return {"status": "healthy", "service": "ia"}

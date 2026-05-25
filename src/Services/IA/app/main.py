

import logging
import os
from contextlib import asynccontextmanager

from dotenv import load_dotenv
from fastapi import FastAPI
import uvicorn

from app.infrastructure.rabbitmq import RabbitMqConnection, RabbitMqConsumer
from app.infrastructure.database import init_db

PORT: int = int(os.getenv("PORT", "5004"))
HOST: str = os.getenv("HOST", "0.0.0.0")

env_path: str = os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".env")
if os.path.exists(env_path):
    load_dotenv(env_path)

if __name__ == "__main__":
    uvicorn.run(app, host=HOST, port=PORT)

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)

logger: logging.Logger = logging.getLogger(__name__)


RABBITMQ_HOST: str = os.getenv("RabbitMQ__Host", "localhost")
RABBITMQ_USERNAME: str | None = os.getenv("RabbitMQ__Username")
RABBITMQ_PASSWORD: str | None = os.getenv("RabbitMQ__Password")

if not RABBITMQ_USERNAME or not RABBITMQ_PASSWORD:
    raise ValueError("RabbitMQ__Username and RabbitMQ__Password must be configured in the .env file")


consumer: RabbitMqConsumer | None = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    global consumer
    
    # Initialize database
    try:
        init_db()
        logger.info("Database initialized successfully")
    except Exception as e:
        logger.error(f"Failed to initialize database: {e}")
        raise
    
    # Start RabbitMQ consumer
    connection = RabbitMqConnection(
        host=RABBITMQ_HOST,
        username=RABBITMQ_USERNAME,
        password=RABBITMQ_PASSWORD,
    )
    consumer = RabbitMqConsumer(connection)
    consumer.start()
    logger.info("PixPro IA Service started - Multi-model AI processor ready")

    yield

    if consumer:
        consumer.stop()
    logger.info("PixPro IA Service stopped")


app = FastAPI(
    title="PixPro IA Service",
    description="Image Processing Microservice with Multi-Model AI Support (Stable Diffusion, FLUX, OpenAI)",
    version="2.0.0",
    lifespan=lifespan,
)


@app.get("/health")
async def health():
    return {"status": "healthy", "service": "ia"}

import logging
import os
import time
import uuid
from abc import ABC, abstractmethod
from datetime import datetime, timezone

import requests

from app.events import ProcessingResult

logger = logging.getLogger(__name__)


class ImageProcessor(ABC):
    @abstractmethod
    def process(self, image_url: str) -> tuple[str, ProcessingResult]:
        pass


class MockAiImageProcessor(ImageProcessor):
    def process(self, image_url: str) -> tuple[str, ProcessingResult]:
        logger.info("  starting image processing for %s", image_url)

        time.sleep(2)

        process_id = uuid.uuid4().hex[:12]
        processed_url = f"{image_url}?processed=true&filter=ai-enhanced&pid={process_id}"

        results = {
            "filter": "ai-enhanced",
            "confidence": 0.95,
            "detections": ["object_a", "object_b"],
            "processedAt": datetime.now(timezone.utc).isoformat(),
        }

        logger.info("  processing complete result url %s", processed_url)

        return processed_url, results


class HuggingFaceImageProcessor(ImageProcessor):
    CLASSIFICATION_API = (
        "https://api-inference.huggingface.co/models/google/vit-base-patch16-224"
    )

    def __init__(self, api_token: str):
        self._headers = {"Authorization": f"Bearer {api_token}"}

    def process(self, image_url: str) -> tuple[str, ProcessingResult]:
        logger.info("  starting hugging face processing for %s", image_url)

        image_response = requests.get(image_url, timeout=30)
        image_response.raise_for_status()

        response = requests.post(
            self.CLASSIFICATION_API,
            headers=self._headers,
            data=image_response.content,
            timeout=60,
        )
        response.raise_for_status()

        predictions = response.json()

        results = {
            "filter": "huggingface-vit-classification",
            "confidence": predictions[0]["score"] if predictions else 0.0,
            "detections": [p["label"] for p in predictions[:5]],
            "processedAt": datetime.now(timezone.utc).isoformat(),
        }

        logger.info("  processing complete for %s", image_url)

        return image_url, results


def build_processor() -> ImageProcessor:
    hf_token = os.getenv("HF_TOKEN")
    if hf_token:
        logger.info("AI mode: HuggingFace (google/vit-base-patch16-224)")
        return HuggingFaceImageProcessor(api_token=hf_token)
    logger.info("AI mode: Mock HF_TOKEN not configured  so it just a mock xd")
    return MockAiImageProcessor()

import os
import time
import base64
import logging
from datetime import datetime, timezone
from typing import List, Tuple

import requests

from app.processors.base import ImageProcessor
from app.models.events import ProcessingParameters, ProcessingResult
from app.infrastructure.cloudinary import upload_image

logger = logging.getLogger(__name__)


class PixazoProcessor(ImageProcessor):
    """Processor using Pixazo API for text-to-image generation.

    Cheapest option: Flux Schnell at ~$0.0012 per image.
    Synchronous API - returns image directly without polling.

    Endpoint: POST https://gateway.pixazo.ai/flux-1-schnell/v1/getData
    Auth: Ocp-Apim-Subscription-Key: {PIXAZO_API_KEY}
    """

    def __init__(
        self,
        api_key: str | None = None,
        model: str = "flux-schnell",
    ):
        self._api_key = api_key or os.getenv("PIXAZO_API_KEY")
        if not self._api_key:
            raise ValueError("PIXAZO_API_KEY is required for Pixazo processing")

        self._model_key = model
        self._endpoint = "https://gateway.pixazo.ai/flux-1-schnell/v1/getData"

        logger.info(
            "Pixazo processor initialized (model=%s, endpoint=%s)",
            model,
            self._endpoint,
        )

    @property
    def model_name(self) -> str:
        return self._model_key

    def process(
        self,
        image_url: str | None,
        prompt: str,
        parameters: ProcessingParameters,
    ) -> Tuple[List[str], ProcessingResult]:
        """Generate images from text prompt using Pixazo Flux Schnell.

        Note: image_url is ignored for text-to-image generation.
        """
        start_time = time.time()
        logger.info(
            "Generating image with Pixazo %s: prompt='%s'", self._model_key, prompt[:50]
        )

        headers = {
            "Ocp-Apim-Subscription-Key": self._api_key,
            "Content-Type": "application/json",
            "Cache-Control": "no-cache",
        }

        processed_urls: List[str] = []

        for i in range(parameters.quantity):
            try:
                # Flux Schnell uses fewer steps for faster generation
                payload = {
                    "prompt": prompt,
                    "num_steps": 4,  # Schnell uses 4 steps (fastest)
                    "height": parameters.height or 512,
                    "width": parameters.width or 512,
                }

                response = requests.post(
                    self._endpoint,
                    headers=headers,
                    json=payload,
                    timeout=60,
                )

                if response.status_code != 200:
                    raise RuntimeError(
                        f"Pixazo API error {response.status_code}: {response.text[:300]}"
                    )

                # Check if response is JSON (contains URL) or binary (image data)
                content_type = response.headers.get("Content-Type", "")

                if "application/json" in content_type:
                    # Pixazo returns JSON with output URL
                    body = response.json()
                    if "output" in body:
                        # Download image from the URL
                        image_url = body["output"]
                        logger.info("Downloading image from Pixazo URL: %s", image_url)
                        img_response = requests.get(image_url, timeout=60)
                        if img_response.status_code != 200:
                            raise RuntimeError(
                                f"Failed to download image from {image_url}: {img_response.status_code}"
                            )
                        result_bytes = img_response.content
                    elif "request_id" in body:
                        # Async response - would need polling (not typical for Schnell)
                        raise RuntimeError(
                            f"Pixazo returned async response for sync endpoint: {body}"
                        )
                    else:
                        raise RuntimeError(f"Pixazo API error: {body}")
                else:
                    # Binary image data (direct response)
                    result_bytes = response.content

                if not result_bytes:
                    raise RuntimeError("Pixazo returned empty image data")

                public_id = f"processed_pixazo_{int(time.time())}_{i}"
                uploaded_url = upload_image(result_bytes, public_id=public_id)
                processed_urls.append(uploaded_url)

                logger.info(
                    "Generated image %d/%d: %s",
                    i + 1,
                    parameters.quantity,
                    uploaded_url,
                )

            except Exception as e:
                logger.error("Error generating image %d with Pixazo: %s", i + 1, e)
                if i == 0:
                    raise

        processing_time = int((time.time() - start_time) * 1000)

        result = ProcessingResult(
            model=self._model_key,
            prompt=prompt,
            parameters=parameters,
            processed_urls=processed_urls,
            processing_time_ms=processing_time,
            processed_at=datetime.now(timezone.utc),
        )

        logger.info(
            "Pixazo processing complete in %dms, generated %d images",
            processing_time,
            len(processed_urls),
        )

        return processed_urls, result

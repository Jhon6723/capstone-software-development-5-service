import os
import time
import base64
import logging
from datetime import datetime, timezone
from typing import List, Tuple

import requests

from app.processors.base import ImageProcessor
from app.models.events import ProcessingParameters, ProcessingResult
from app.infrastructure.cloudinary import upload_image, download_image

logger = logging.getLogger(__name__)


class PollinationsProcessor(ImageProcessor):
    """Processor using Pollinations.ai OpenAI-compatible image-edits endpoint.

    Default tier model: ``klein`` (FLUX.2 Klein 4B) ~$0.01/image.
    Other supported img2img-capable models: ``kontext``, ``qwen-image``,
    ``wan-image``, ``gptimage``, ``gptimage-large``.

    Endpoint: POST {POLLINATIONS_ENDPOINT}/v1/images/edits
    Auth:     Authorization: Bearer {POLLINATIONS_API_KEY}
    """

    def __init__(
        self,
        api_key: str | None = None,
        endpoint: str | None = None,
        model: str = "klein",
    ):
        self._api_key = api_key or os.getenv("POLLINATIONS_API_KEY")
        if not self._api_key:
            raise ValueError("POLLINATIONS_API_KEY is required for Pollinations processing")

        self._endpoint = endpoint or os.getenv("POLLINATIONS_ENDPOINT")
        if not self._endpoint:
            raise ValueError("POLLINATIONS_ENDPOINT is required for Pollinations processing")

        self._model_key = model
        logger.info(
            "Pollinations processor initialized (model=%s, endpoint=%s)",
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
        start_time = time.time()

        if not image_url:
            raise ValueError("Pollinations processor requires ImageUrl for image-to-image editing")

        logger.info(
            "Processing image with Pollinations %s: %s", self._model_key, image_url
        )

        # Pollinations /v1/images/edits accepts source images either as direct
        # URLs in JSON or as multipart uploads. The simplest, lowest-bandwidth
        # path is to pass the original URL straight through.
        url = self._endpoint.rstrip("/") + "/v1/images/edits"
        headers = {
            "Authorization": f"Bearer {self._api_key}",
            "Content-Type": "application/json",
        }

        processed_urls: List[str] = []

        for i in range(parameters.quantity):
            try:
                payload = {
                    "model": self._model_key,
                    "prompt": prompt,
                    "image": image_url,
                    "size": f"{parameters.width}x{parameters.height}",
                    "n": 1,
                    "response_format": "b64_json",
                }

                response = requests.post(url, headers=headers, json=payload, timeout=180)

                if response.status_code != 200:
                    raise RuntimeError(
                        f"Pollinations API error {response.status_code}: {response.text[:300]}"
                    )

                body = response.json()
                data = body.get("data") or []
                if not data:
                    raise RuntimeError(f"Pollinations returned empty data: {body}")

                first = data[0]
                if first.get("b64_json"):
                    result_bytes = base64.b64decode(first["b64_json"])
                elif first.get("url"):
                    result_bytes = download_image(first["url"])
                else:
                    raise RuntimeError(f"Pollinations response has no image payload: {first}")

                public_id = f"processed_pollinations_{int(time.time())}_{i}"
                uploaded_url = upload_image(result_bytes, public_id=public_id)
                processed_urls.append(uploaded_url)

                logger.info(
                    "Generated image %d/%d: %s",
                    i + 1,
                    parameters.quantity,
                    uploaded_url,
                )

            except Exception as e:
                logger.error("Error generating image %d with Pollinations: %s", i + 1, e)
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
            "Pollinations processing complete in %dms, generated %d images",
            processing_time,
            len(processed_urls),
        )

        return processed_urls, result

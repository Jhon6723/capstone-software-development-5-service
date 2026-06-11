import os
import time
import logging
from datetime import datetime, timezone
from typing import List, Tuple

import requests

from app.processors.base import ImageProcessor, ModelTier
from app.models.events import ProcessingParameters, ProcessingResult
from app.infrastructure.cloudinary import upload_image, download_image

logger = logging.getLogger(__name__)

_BASE_URL      = "https://api.nanobananaapi.ai"
_V1_GENERATE   = f"{_BASE_URL}/api/v1/nanobanana/generate"
_V2_GENERATE   = f"{_BASE_URL}/api/v1/nanobanana/generate-2"
_PRO_GENERATE  = f"{_BASE_URL}/api/v1/nanobanana/generate-pro"
_TASK_STATUS   = f"{_BASE_URL}/api/v1/nanobanana/record-info"

_POLL_INTERVAL_S  = 3
_POLL_MAX_RETRIES = 60   # 3s × 60 = 3 min max wait

_SUCCESS_FLAG = 1
_GENERATING_FLAG = 0

_TIER_ENDPOINT: dict[str, str] = {
    ModelTier.NB_LOW.value:    _V1_GENERATE,
    ModelTier.NB_MEDIUM.value: _V2_GENERATE,
    ModelTier.NB_MAX.value:    _PRO_GENERATE,
}


class NanaBananaProcessor(ImageProcessor):
    """Processor using NanaBanana API for instruction-based image editing.

    All three tiers are asynchronous: POST returns a taskId, then we poll
    GET /api/v1/nanobanana/record-info?taskId=<id> until successFlag=1.

    Tier mapping:
      nanobanana-low    → V1 /generate   (Gemini 2.5 Flash)  ~$0.020/img
      nanobanana-medium → V2 /generate-2 (Gemini 3.1 Flash)  ~$0.040/img
      nanobanana-max    → Pro /generate-pro (Gemini 3 Pro)    ~$0.090/img
    """

    def __init__(
        self,
        tier: str = ModelTier.NB_LOW.value,
        api_key: str | None = None,
    ):
        self._api_key = api_key or os.getenv("NANOBANANA_API_KEY")
        if not self._api_key:
            raise ValueError("NANOBANANA_API_KEY is required for NanaBanana processing")

        if tier not in _TIER_ENDPOINT:
            raise ValueError(
                f"Unsupported NanaBanana tier '{tier}'. "
                f"Allowed: {list(_TIER_ENDPOINT.keys())}"
            )

        self._tier = tier
        self._endpoint = _TIER_ENDPOINT[tier]
        self._headers = {
            "Authorization": f"Bearer {self._api_key}",
            "Content-Type": "application/json",
        }

        logger.info("NanaBanana processor initialized (tier=%s)", tier)

    @property
    def model_name(self) -> str:
        return self._tier

    def process(
        self,
        image_url: str | None,
        prompt: str,
        parameters: ProcessingParameters,
    ) -> Tuple[List[str], ProcessingResult]:
        start_time = time.time()

        if not image_url:
            raise ValueError("NanaBanana processor requires ImageUrl for image-to-image editing")

        logger.info("Processing image with NanaBanana %s: %s", self._tier, image_url)

        processed_urls: List[str] = []

        for i in range(parameters.quantity):
            try:
                payload = self._build_payload(image_url, prompt, parameters)

                response = requests.post(
                    self._endpoint, headers=self._headers, json=payload, timeout=60
                )

                if response.status_code != 200:
                    raise RuntimeError(
                        f"NanaBanana submit error {response.status_code}: {response.text[:300]}"
                    )

                body = response.json()
                task_id = body.get("data", {}).get("taskId")
                if not task_id:
                    raise RuntimeError(f"NanaBanana response missing taskId: {body}")

                logger.info("NanaBanana task submitted (taskId=%s), polling...", task_id)

                result_url = self._poll_for_result(task_id)

                result_bytes = download_image(result_url)
                public_id = f"processed_nanobanana_{self._tier}_{int(time.time())}_{i}"
                uploaded_url = upload_image(result_bytes, public_id=public_id)
                processed_urls.append(uploaded_url)

                logger.info("Generated image %d/%d: %s", i + 1, parameters.quantity, uploaded_url)

            except Exception as e:
                logger.error("Error generating image %d with NanaBanana: %s", i + 1, e)
                if i == 0:
                    raise

        processing_time = int((time.time() - start_time) * 1000)

        result = ProcessingResult(
            model=self._tier,
            prompt=prompt,
            parameters=parameters,
            processed_urls=processed_urls,
            processing_time_ms=processing_time,
            processed_at=datetime.now(timezone.utc),
        )

        logger.info(
            "NanaBanana processing complete in %dms, generated %d images",
            processing_time, len(processed_urls),
        )

        return processed_urls, result

    def _poll_for_result(self, task_id: str) -> str:
        """Poll task status until successFlag=1, then return resultImageUrl."""
        for attempt in range(_POLL_MAX_RETRIES):
            time.sleep(_POLL_INTERVAL_S)

            resp = requests.get(
                _TASK_STATUS,
                headers=self._headers,
                params={"taskId": task_id},
                timeout=30,
            )

            if resp.status_code != 200:
                raise RuntimeError(
                    f"NanaBanana poll error {resp.status_code}: {resp.text[:300]}"
                )

            data = resp.json().get("data", {})
            flag = data.get("successFlag")

            if flag == _SUCCESS_FLAG:
                result_url = data.get("response", {}).get("resultImageUrl")
                if not result_url:
                    raise RuntimeError(f"NanaBanana task succeeded but no resultImageUrl: {data}")
                logger.info("NanaBanana task %s completed (attempt %d)", task_id, attempt + 1)
                return result_url

            if flag not in (_GENERATING_FLAG, None):
                error_msg = data.get("errorMessage", "unknown error")
                raise RuntimeError(
                    f"NanaBanana task {task_id} failed (flag={flag}): {error_msg}"
                )

            logger.debug("NanaBanana task %s still generating (attempt %d/%d)", task_id, attempt + 1, _POLL_MAX_RETRIES)

        raise RuntimeError(
            f"NanaBanana task {task_id} timed out after {_POLL_MAX_RETRIES * _POLL_INTERVAL_S}s"
        )

    def _build_payload(
        self,
        image_url: str,
        prompt: str,
        parameters: ProcessingParameters,
    ) -> dict:
        """Build the request payload for the selected tier endpoint."""
        aspect_ratio = self._size_to_aspect_ratio(parameters.width, parameters.height)

        if self._tier == ModelTier.NB_LOW.value:
            return {
                "type": "IMAGETOIAMGE",
                "prompt": prompt,
                "imageUrls": [image_url],
                "numImages": 1,
                "image_size": aspect_ratio,
            }
        if self._tier == ModelTier.NB_MEDIUM.value:
            return {
                "prompt": prompt,
                "imageUrls": [image_url],
                "aspectRatio": aspect_ratio,
                "resolution": "1K",
                "outputFormat": "jpg",
            }
        # NB_MAX (Pro)
        return {
            "prompt": prompt,
            "imageUrls": [image_url],
            "aspectRatio": aspect_ratio,
            "resolution": "2K",
        }

    @staticmethod
    def _size_to_aspect_ratio(width: int, height: int) -> str:
        """Map width/height to the closest NanaBanana supported aspect ratio."""
        ratio = width / height if height else 1.0
        if ratio > 1.6:
            return "16:9"
        if ratio > 1.2:
            return "4:3"
        if ratio < 0.65:
            return "9:16"
        if ratio < 0.85:
            return "3:4"
        return "1:1"

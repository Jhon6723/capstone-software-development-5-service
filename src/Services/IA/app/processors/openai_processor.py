import os
import time
import base64
import logging
from datetime import datetime, timezone
from typing import List, Tuple
from io import BytesIO

from openai import OpenAI
from PIL import Image

from app.processors.base import ImageProcessor
from app.models.events import ProcessingParameters, ProcessingResult
from app.infrastructure.cloudinary import upload_image, download_image

logger = logging.getLogger(__name__)


class OpenAIProcessor(ImageProcessor):
    """Processor using OpenAI GPT Image API for image editing"""
    
    SUPPORTED_QUALITIES = {"low", "medium", "high", "auto"}

    def __init__(
        self,
        api_key: str | None = None,
        model: str = "gpt-image-1.5",
        quality: str = "low",
    ):
        self._api_key = api_key or os.getenv("OPENAI_API_KEY")
        if not self._api_key:
            raise ValueError("OPENAI_API_KEY is required for OpenAI processing")

        if quality not in self.SUPPORTED_QUALITIES:
            raise ValueError(
                f"Unsupported OpenAI quality '{quality}'. Allowed: {self.SUPPORTED_QUALITIES}"
            )

        self._model = model
        self._quality = quality
        self._client = OpenAI(api_key=self._api_key)

        logger.info("OpenAI processor initialized (model=%s, quality=%s)", model, quality)

    
    @property
    def model_name(self) -> str:
        return self._model
    
    def process(
        self,
        image_url: str | None,
        prompt: str,
        parameters: ProcessingParameters
    ) -> Tuple[List[str], ProcessingResult]:
        start_time = time.time()

        if not image_url:
            raise ValueError("OpenAI processor requires ImageUrl for image-to-image editing")

        logger.info(f"Processing image with OpenAI {self._model}: {image_url}")

        # Download original image
        image_data = download_image(image_url)
        
        # OpenAI gpt-image-1 supports a fixed set of sizes.
        # Pick the closest one based on aspect ratio.
        size = self._closest_supported_size(parameters.width, parameters.height)
        
        processed_urls = []
        
        # OpenAI's image editing API
        for i in range(parameters.quantity):
            try:
                # Convert to PNG (RGBA) so OpenAI accepts it. The SDK infers
                # the mimetype from the file's `name` attribute.
                img = Image.open(BytesIO(image_data)).convert("RGBA")
                image_file = BytesIO()
                img.save(image_file, format="PNG")
                image_file.seek(0)
                image_file.name = "image.png"
                
                response = self._client.images.edit(
                    image=image_file,
                    prompt=prompt,
                    n=1,
                    size=size,
                    model=self._model,
                    quality=self._quality,
                )
                
                # gpt-image-1 returns base64 in `b64_json`; older models return `url`.
                data_item = response.data[0]
                if getattr(data_item, "b64_json", None):
                    result_data = base64.b64decode(data_item.b64_json)
                elif getattr(data_item, "url", None):
                    result_data = download_image(data_item.url)
                else:
                    raise RuntimeError("OpenAI response contained neither b64_json nor url")
                
                # Upload to Cloudinary
                public_id = f"processed_openai_{int(time.time())}_{i}"
                uploaded_url = upload_image(result_data, public_id=public_id)
                processed_urls.append(uploaded_url)
                
                logger.info(f"Generated image {i+1}/{parameters.quantity}: {uploaded_url}")
                
            except Exception as e:
                logger.error(f"Error generating image with OpenAI: {e}")
                if i == 0:
                    raise
        
        processing_time = int((time.time() - start_time) * 1000)
        
        result = ProcessingResult(
            model=self._model,
            prompt=prompt,
            parameters=parameters,
            processed_urls=processed_urls,
            processing_time_ms=processing_time,
            processed_at=datetime.now(timezone.utc),
        )
        
        logger.info(f"OpenAI processing complete in {processing_time}ms, generated {len(processed_urls)} images")
        
        return processed_urls, result

    @staticmethod
    def _closest_supported_size(width: int, height: int) -> str:
        """Map an arbitrary (width, height) to the closest OpenAI-supported size.

        gpt-image-1 only accepts: 1024x1024, 1024x1536 (portrait), 1536x1024 (landscape).
        """
        ratio = width / height if height else 1.0
        if ratio > 1.2:
            return "1536x1024"  # landscape
        if ratio < 0.83:
            return "1024x1536"  # portrait
        return "1024x1024"      # square

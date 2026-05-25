import time
import logging
from datetime import datetime, timezone
from typing import List, Tuple
from io import BytesIO

from PIL import Image, ImageFilter, ImageEnhance

from app.processors.base import ImageProcessor
from app.models.events import ProcessingParameters, ProcessingResult
from app.infrastructure.cloudinary import upload_image, download_image

logger = logging.getLogger(__name__)


class MockImageProcessor(ImageProcessor):
    """Mock processor for testing - applies simple image filters"""
    
    def __init__(self):
        logger.info("MockImageProcessor initialized - no AI, just filters")
    
    @property
    def model_name(self) -> str:
        return "mock-filter-processor"
    
    def process(
        self,
        image_url: str | None,
        prompt: str,
        parameters: ProcessingParameters
    ) -> Tuple[List[str], ProcessingResult]:
        start_time = time.time()

        if not image_url:
            raise ValueError("MockImageProcessor requires ImageUrl for image processing")

        logger.info(f"Mock processing image: {image_url}")
        logger.info(f"Prompt (ignored in mock): {prompt}")

        # Download original image
        image_data = download_image(image_url)
        
        processed_urls = []
        
        for i in range(parameters.quantity):
            # Open image with PIL
            img = Image.open(BytesIO(image_data))
            
            # Apply simple filters to simulate processing
            # Apply blur
            img = img.filter(ImageFilter.GaussianBlur(radius=2))
            
            # Enhance color
            enhancer = ImageEnhance.Color(img)
            img = enhancer.enhance(1.5)
            
            # Enhance contrast
            enhancer = ImageEnhance.Contrast(img)
            img = enhancer.enhance(1.2)
            
            # Convert to bytes
            img_byte_arr = BytesIO()
            img.save(img_byte_arr, format='PNG')
            img_byte_arr.seek(0)
            
            # Upload to Cloudinary
            public_id = f"mock_processed_{int(time.time())}_{i}"
            uploaded_url = upload_image(img_byte_arr.getvalue(), public_id=public_id)
            processed_urls.append(uploaded_url)
            
            logger.info(f"Mock generated image {i+1}/{parameters.quantity}")
        
        processing_time = int((time.time() - start_time) * 1000)
        
        result = ProcessingResult(
            model="mock-filter-processor",
            prompt=prompt,
            parameters=parameters,
            processed_urls=processed_urls,
            processing_time_ms=processing_time,
            processed_at=datetime.now(timezone.utc),
        )
        
        logger.info(f"Mock processing complete in {processing_time}ms")
        
        return processed_urls, result

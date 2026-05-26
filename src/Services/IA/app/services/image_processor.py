import logging
from typing import Tuple, List

from app.processors.base import ImageProcessor, ModelTier
from app.processors.openai_processor import OpenAIProcessor
from app.processors.pollinations import PollinationsProcessor
from app.processors.pixazo import PixazoProcessor
from app.processors.mock import MockImageProcessor
from app.models.events import ProcessingParameters, ProcessingResult, ImageUploadedEvent, ProcessingFeature
from app.services.cloudinary_service import CloudinaryService
from app.services.guardrails import Guardrails, ContentModerationError, get_guardrails

logger = logging.getLogger(__name__)


class ImageProcessingService:
    """Orchestrates image processing workflow"""
    
    def __init__(self):
        self._cloudinary_service = CloudinaryService()
        self._guardrails = get_guardrails()
    
    def _get_processor(self, model: str | None = None) -> ImageProcessor:
        """Get the appropriate processor for the requested model.

        Routing:
          - DEFAULT      (gpt-image-1-mini-low)  -> OpenAIProcessor      (quality=low, image edit)
          - PIXAZO       (flux-schnell)           -> PixazoProcessor      (text-to-image, cheapest)
          - STANDARD     (kontext)                -> PollinationsProcessor (image-to-image)
          - AI_REASONING (gpt-image-1-mini-high) -> OpenAIProcessor      (quality=high, image edit)
          - Missing creds / unknown model        -> MockImageProcessor   (dev fallback)
        """
        model = model or ModelTier.DEFAULT.value

        try:
            if model == ModelTier.DEFAULT.value:
                return OpenAIProcessor(model="gpt-image-1-mini", quality="low")
            if model == ModelTier.PIXAZO.value:
                return PixazoProcessor(model="flux-schnell")
            if model == ModelTier.AI_REASONING.value:
                return OpenAIProcessor(model="gpt-image-1-mini", quality="high")
            if model == ModelTier.STANDARD.value:
                return PollinationsProcessor(model="kontext")
        except ValueError as e:
            logger.warning("Processor for model '%s' not configured: %s", model, e)

        # Unknown model or missing credentials → fall back to Mock
        logger.info("Using MockImageProcessor (model=%s, no real processor available)", model)
        return MockImageProcessor()
    
    def process(
        self,
        event: ImageUploadedEvent
    ) -> Tuple[List[str], ProcessingResult]:
        """
        Process an image based on the uploaded event
        
        Args:
            event: ImageUploadedEvent containing image URL and processing parameters
            
        Returns:
            Tuple of (list of processed image URLs, processing results)
        """
        # Get parameters with defaults
        parameters = event.Parameters or ProcessingParameters()
        
        # Determine which model to use based on Feature
        # Feature 0 (GENERATOR) = text-to-image -> ALWAYS use flux-schnell (Pixazo)
        # Feature 1 (EDITOR) = image-to-image -> REQUIRES explicit model in parameters
        if event.Feature == ProcessingFeature.GENERATOR:
            # Text-to-image: force Pixazo (flux-schnell) - cheapest, fastest, no source image needed
            model = ModelTier.PIXAZO.value
            logger.info(f"Feature=GENERATOR detected - forcing model: {model} (text-to-image)")
        elif event.Feature == ProcessingFeature.EDITOR:
            # Image-to-image: model is REQUIRED
            if event.Parameters and event.Parameters.model:
                model = event.Parameters.model
                logger.info(f"Feature=EDITOR detected - using specified model: {model}")
            else:
                raise ValueError("Feature=EDITOR requires explicit 'model' in Parameters. "
                               "Valid models: gpt-image-1-mini-low, gpt-image-1-mini-high, kontext")
        else:
            # Fallback for unknown feature values
            model = ModelTier.DEFAULT.value
            logger.warning(f"Unknown Feature value {event.Feature}, using default model: {model}")
        
        # Check content safety before processing
        try:
            self._guardrails.check_prompt(event.Prompt)
        except ContentModerationError as e:
            logger.error(f"Content moderation failed for image {event.ImageId}: {e}")
            raise RuntimeError(f"Content blocked by safety filter: {e.reason}") from e

        logger.info(f"Starting processing for image {event.ImageId} with model {model}")

        # Get appropriate processor
        processor = self._get_processor(model)
        
        # Process the image
        processed_urls, results = processor.process(
            image_url=event.ImageUrl,
            prompt=event.Prompt,
            parameters=parameters
        )
        
        logger.info(f"Processing complete for image {event.ImageId}: {len(processed_urls)} images generated")
        
        return processed_urls, results

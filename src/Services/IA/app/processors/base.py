from abc import ABC, abstractmethod
from enum import Enum
from typing import Tuple, List
from app.models.events import ProcessingParameters, ProcessingResult


class ModelTier(str, Enum):
    """Available AI model tiers"""
    PIXAZO     = "flux-schnell"          # Pixazo Flux Schnell (~$0.0012/img) - text-to-image only
    DEFAULT    = "kontext"               # Pollinations FLUX.1 Kontext (~$0.005/img) - default img2img
    GPT15_LOW  = "gpt-image-1.5-low"    # OpenAI gpt-image-1, quality=low (~$0.009/img)
    NB_LOW     = "nanobanana-low"        # NanaBanana V1 Gemini 2.5 Flash (~$0.020/img)
    NB_MEDIUM  = "nanobanana-medium"     # NanaBanana V2 Gemini 3.1 Flash (~$0.040/img)
    NB_MAX     = "nanobanana-max"        # NanaBanana Pro Gemini 3 Pro (~$0.090/img)
    GPT15_MED  = "gpt-image-1.5-medium" # OpenAI gpt-image-1, quality=medium (~$0.034/img)


class ImageProcessor(ABC):
    """Base class for image processors"""

    @abstractmethod
    def process(
        self,
        image_url: str | None,
        prompt: str,
        parameters: ProcessingParameters
    ) -> Tuple[List[str], ProcessingResult]:
        """
        Process an image using AI

        Args:
            image_url: URL of the image to process (None for text-to-image)
            prompt: Text prompt for the AI
            parameters: Processing parameters

        Returns:
            Tuple of (list of processed image URLs, processing results)
        """
        pass
    
    @property
    @abstractmethod
    def model_name(self) -> str:
        """Return the name of the model"""
        pass

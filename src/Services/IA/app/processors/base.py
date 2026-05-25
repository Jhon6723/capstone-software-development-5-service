from abc import ABC, abstractmethod
from enum import Enum
from typing import Tuple, List
from app.models.events import ProcessingParameters, ProcessingResult


class ModelTier(str, Enum):
    """Available AI model tiers"""
    DEFAULT = "gpt-image-1-mini-low"     # OpenAI gpt-image-1-mini, quality=low (~$0.011/img)
    PIXAZO = "flux-schnell"              # Pixazo Flux Schnell (~$0.0012/img) - Cheapest text-to-image
    STANDARD = "kontext"                 # Pollinations FLUX.1 Kontext (~$0.04/img)
    AI_REASONING = "gpt-image-1-mini-high"  # OpenAI gpt-image-1-mini, quality=high (~$0.167/img)


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

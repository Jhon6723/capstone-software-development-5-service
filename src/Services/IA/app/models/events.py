from datetime import datetime
from enum import IntEnum
from typing import Optional, Union
from pydantic import BaseModel, Field, field_validator


class ProcessingFeature(IntEnum):
    """AI processing feature type"""
    GENERATOR = 0  # Text-to-image generation
    EDITOR = 1      # Image-to-image editing

    @classmethod
    def _missing_(cls, value):
        """Handle both int and string inputs"""
        if isinstance(value, str):
            value = value.upper()
            for member in cls:
                if member.name == value:
                    return member
        return None


class ProcessingParameters(BaseModel):
    """Parameters for image processing"""
    width: int = Field(default=512, ge=64, le=2048, alias="Width")
    height: int = Field(default=512, ge=64, le=2048, alias="Height")
    num_inference_steps: int = Field(default=20, ge=1, le=100, alias="NumInferenceSteps")
    strength: float = Field(default=0.75, ge=0.0, le=1.0, alias="Strength")
    guidance_scale: float = Field(default=7.5, ge=1.0, le=20.0, alias="GuidanceScale")
    quantity: int = Field(default=1, ge=1, le=10, alias="Quantity")
    model: str = Field(default="gpt-image-1-mini-low", alias="Model")

    class Config:
        populate_by_name = True


class ProcessingResult(BaseModel):
    """Results from image processing"""
    model: str
    prompt: str
    parameters: ProcessingParameters
    processed_urls: list[str]
    processing_time_ms: int
    processed_at: datetime


class ImageUploadedEvent(BaseModel):
    """Event triggered when an image is uploaded for processing"""
    ImageId: str = Field(alias="ImageId")
    OwnerId: str = Field(alias="OwnerId")
    ImageUrl: Optional[str] = Field(default=None, alias="ImageUrl")
    Prompt: str = Field(default="", alias="Prompt")
    Feature: ProcessingFeature = Field(default=ProcessingFeature.GENERATOR, alias="Feature")
    Parameters: Optional[ProcessingParameters] = Field(default=None, alias="Parameters")
    UploadedAt: Optional[datetime] = Field(default=None, alias="UploadedAt")

    class Config:
        populate_by_name = True


class ImageProcessingCompletedEvent(BaseModel):
    """Event triggered when image processing is complete"""
    ImageId: str
    UserId: str
    ImageUrl: Optional[str] = Field(default=None)
    ProcessedImageUrls: list[str]
    ProcessingResults: Optional[ProcessingResult] = None
    CompletedAt: datetime


class ImageProcessingFailedEvent(BaseModel):
    """Event triggered when image processing fails"""
    ImageId: str
    UserId: str
    ImageUrl: Optional[str] = Field(default=None)
    ErrorMessage: str
    ErrorCode: str
    FailedAt: datetime

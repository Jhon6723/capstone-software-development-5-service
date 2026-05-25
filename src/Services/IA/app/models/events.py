from datetime import datetime
from typing import Optional
from pydantic import BaseModel, Field


class ProcessingParameters(BaseModel):
    """Parameters for image processing"""
    width: int = Field(default=512, ge=64, le=2048)
    height: int = Field(default=512, ge=64, le=2048)
    num_inference_steps: int = Field(default=20, ge=1, le=100)
    strength: float = Field(default=0.75, ge=0.0, le=1.0)
    guidance_scale: float = Field(default=7.5, ge=1.0, le=20.0)
    quantity: int = Field(default=1, ge=1, le=10)
    model: str = Field(default="stable-diffusion-v1-5")


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
    ImageId: str
    OwnerId: str
    ImageUrl: Optional[str] = Field(default=None)
    Prompt: str = Field(default="")
    Parameters: Optional[ProcessingParameters] = None
    UploadedAt: Optional[datetime] = None


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

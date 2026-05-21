from datetime import datetime
from typing import Optional, TypedDict
from pydantic import BaseModel


class ProcessingResult(TypedDict):
    filter: str
    confidence: float
    detections: list[str]
    processedAt: str


class ImageUploadedEvent(BaseModel):
    ImageId: str
    OwnerId: str
    ImageUrl: str


class ImageProcessingCompletedEvent(BaseModel):
    ImageId: str
    UserId: str
    ImageUrl: str
    ProcessedImageUrl: str
    ProcessingResults: ProcessingResult | None = None
    CompletedAt: datetime


class ImageProcessingFailedEvent(BaseModel):
    ImageId: str
    UserId: str
    ImageUrl: str
    ErrorMessage: str
    ErrorCode: str
    FailedAt: datetime

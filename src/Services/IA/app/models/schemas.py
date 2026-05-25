from datetime import datetime
from typing import Optional
from pydantic import BaseModel, Field
from enum import Enum

# Type alias for flexible JSON fields (simplified to avoid recursion)
JsonDict = dict[str, object]


class JobStatus(str, Enum):
    PENDING = "pending"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"


class JobCreate(BaseModel):
    """Schema for creating a new processing job"""
    original_image_id: str
    original_image_url: Optional[str] = Field(default=None)
    user_id: str
    prompt: str = Field(default="")
    parameters: Optional[JsonDict] = None


class JobResponse(BaseModel):
    """Schema for processing job response"""
    id: str
    original_image_id: str
    original_image_url: Optional[str] = Field(default=None)
    user_id: str
    prompt: str
    parameters: Optional[JsonDict]
    processed_image_urls: Optional[list[str]]
    processing_results: Optional[JsonDict]
    status: JobStatus
    error_message: Optional[str]
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True

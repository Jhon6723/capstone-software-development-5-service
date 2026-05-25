from datetime import datetime
from typing import Optional
from sqlalchemy import Column, String, DateTime, JSON, Integer, Float, func
from sqlalchemy.dialects.postgresql import UUID
from sqlalchemy.orm import declarative_base
import uuid

Base = declarative_base()


class ProcessingJob(Base):
    """Database model for image processing jobs"""
    __tablename__ = "processing_jobs"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    original_image_id = Column(String(255), nullable=False)
    original_image_url = Column(String(1024), nullable=True)
    user_id = Column(UUID(as_uuid=True), nullable=False)
    
    # AI processing parameters
    prompt = Column(String(2048), default="")
    parameters = Column(JSON, default=dict)
    
    # Processing results
    processed_image_urls = Column(JSON, default=list)
    processing_results = Column(JSON, default=dict)
    
    # Job status
    status = Column(String(50), default="pending")
    error_message = Column(String(2048), nullable=True)
    
    # Timestamps
    created_at = Column(DateTime, default=func.now())
    updated_at = Column(DateTime, default=func.now(), onupdate=func.now())

    def __repr__(self):
        return f"<ProcessingJob(id={self.id}, status={self.status}, user_id={self.user_id})>"

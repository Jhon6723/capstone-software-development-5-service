import logging
from typing import Optional, List
from sqlalchemy.orm import Session
from uuid import UUID

from app.models.database import ProcessingJob
from app.models.schemas import JobStatus, JsonDict

logger = logging.getLogger(__name__)


class DatabaseService:
    """Service for database operations"""
    
    def __init__(self, db: Session):
        self._db = db
    
    def create_job(
        self,
        original_image_id: str,
        user_id: str,
        original_image_url: Optional[str] = None,
        prompt: str = "",
        parameters: Optional[JsonDict] = None
    ) -> ProcessingJob:
        """Create a new processing job"""
        job = ProcessingJob(
            original_image_id=original_image_id,
            original_image_url=original_image_url,
            user_id=user_id,
            prompt=prompt,
            parameters=parameters or {},
            status=JobStatus.PENDING.value,
        )
        self._db.add(job)
        self._db.commit()
        self._db.refresh(job)
        logger.info(f"Created processing job {job.id} for image {original_image_id}")
        return job
    
    def update_job_status(
        self,
        job_id: UUID,
        status: JobStatus,
        processed_urls: Optional[List[str]] = None,
        processing_results: Optional[JsonDict] = None,
        error_message: Optional[str] = None
    ) -> Optional[ProcessingJob]:
        """Update job status and results"""
        job = self._db.query(ProcessingJob).filter(ProcessingJob.id == job_id).first()
        if not job:
            logger.warning(f"Job {job_id} not found")
            return None
        
        job.status = status.value
        
        if processed_urls is not None:
            job.processed_image_urls = processed_urls
        
        if processing_results is not None:
            job.processing_results = processing_results
        
        if error_message is not None:
            job.error_message = error_message
        
        self._db.commit()
        self._db.refresh(job)
        logger.info(f"Updated job {job_id} status to {status.value}")
        return job
    
    def get_job(self, job_id: UUID) -> Optional[ProcessingJob]:
        """Get a job by ID"""
        return self._db.query(ProcessingJob).filter(ProcessingJob.id == job_id).first()
    
    def get_jobs_by_user(self, user_id: str, limit: int = 100) -> List[ProcessingJob]:
        """Get all jobs for a user"""
        return (
            self._db.query(ProcessingJob)
            .filter(ProcessingJob.user_id == user_id)
            .order_by(ProcessingJob.created_at.desc())
            .limit(limit)
            .all()
        )

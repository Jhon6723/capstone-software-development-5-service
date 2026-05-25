import logging
from typing import Optional
import cloudinary
from app.infrastructure.cloudinary import upload_image, download_image, get_cloudinary_client

logger = logging.getLogger(__name__)


class CloudinaryService:
    """Service for Cloudinary operations"""
    
    def __init__(self):
        self._client: Optional[cloudinary.Cloudinary] = get_cloudinary_client()
        if not self._client:
            logger.warning("Cloudinary not configured - uploads will fail")
    
    def upload_processed_image(self, image_data: bytes, public_id: Optional[str] = None) -> str:
        """Upload a processed image to Cloudinary"""
        return upload_image(image_data, folder="processed_images", public_id=public_id)
    
    def download_original_image(self, url: str) -> bytes:
        """Download an original image from URL"""
        return download_image(url)
    
    def is_configured(self) -> bool:
        """Check if Cloudinary is properly configured"""
        return self._client is not None

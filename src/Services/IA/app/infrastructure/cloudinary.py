from typing import Optional
import os
import logging
import cloudinary
import cloudinary.uploader
from cloudinary.utils import cloudinary_url

logger = logging.getLogger(__name__)


def get_cloudinary_client() -> Optional[object]:
    """Configure and return Cloudinary module (configured)"""
    cloud_name = os.getenv("CLOUDINARY_CLOUD_NAME")
    api_key = os.getenv("CLOUDINARY_API_KEY")
    api_secret = os.getenv("CLOUDINARY_API_SECRET")
    
    if not all([cloud_name, api_key, api_secret]):
        logger.warning("Cloudinary credentials not fully configured")
        return None
    
    cloudinary.config(
        cloud_name=cloud_name,
        api_key=api_key,
        api_secret=api_secret,
        secure=True
    )
    
    return cloudinary


def upload_image(image_data: bytes, folder: str = "processed_images", public_id: Optional[str] = None) -> str:
    """Upload an image to Cloudinary"""
    client = get_cloudinary_client()
    if not client:
        raise ValueError("Cloudinary not configured")
    
    try:
        result = cloudinary.uploader.upload(
            image_data,
            folder=folder,
            public_id=public_id,
            resource_type="image"
        )
        return result["secure_url"]
    except Exception as e:
        logger.error(f"Failed to upload image to Cloudinary: {e}")
        raise


def download_image(url: str) -> bytes:
    """Download image from URL"""
    import requests
    
    try:
        response = requests.get(url, timeout=30)
        response.raise_for_status()
        return response.content
    except Exception as e:
        logger.error(f"Failed to download image from {url}: {e}")
        raise

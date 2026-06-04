import sys
import os
sys.path.insert(0, os.path.dirname(__file__))

import pytest
from unittest.mock import Mock, patch, AsyncMock


class TestMainApp:
    
    # TEST 1: Verify that the FastAPI lifespan initializes database and RabbitMQ
    # Tests that on application startup, init_db is called and RabbitMQ consumer is started
    @patch('app.main.init_db')
    @patch('app.main.RabbitMqConnection')
    @patch('app.main.RabbitMqConsumer')
    def test_lifespan(self, mock_consumer, mock_connection, mock_init_db):
        from app.main import lifespan
        from fastapi import FastAPI
        
        app = FastAPI()
        
        mock_conn = Mock()
        mock_connection.return_value = mock_conn
        
        mock_consumer_instance = Mock()
        mock_consumer.return_value = mock_consumer_instance
        
        async def run_lifespan():
            async with lifespan(app):
                pass
        
        import asyncio
        asyncio.run(run_lifespan())
        
        mock_init_db.assert_called()
        mock_connection.assert_called()
        mock_consumer.return_value.start.assert_called()
    
    # TEST 2: Verify that the health endpoint returns correct status
    # Tests that GET /health returns 200 OK with service status information
    def test_health_endpoint(self):
        from app.main import app
        from fastapi.testclient import TestClient
        
        client = TestClient(app)
        response = client.get("/health")
        
        assert response.status_code == 200
        assert response.json() == {"status": "healthy", "service": "ia"}
        
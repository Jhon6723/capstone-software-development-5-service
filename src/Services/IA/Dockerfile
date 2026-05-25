FROM python:3.12-slim

WORKDIR /app


COPY src/Services/IA/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY src/Services/IA/app ./app

EXPOSE 8084

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8084"]

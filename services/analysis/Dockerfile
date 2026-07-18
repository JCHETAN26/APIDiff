# syntax=docker/dockerfile:1
FROM python:3.13-slim AS base
WORKDIR /app
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1

COPY pyproject.toml ./
COPY src ./src
COPY gen ./gen
RUN pip install --no-cache-dir .

# Generated gRPC stubs (the `apidiff` package) are on the path but not installed.
ENV PYTHONPATH=/app/gen
EXPOSE 8082
USER nobody
CMD ["python", "-m", "analysis"]

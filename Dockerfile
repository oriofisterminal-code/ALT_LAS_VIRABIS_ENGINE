# ALT_LAS Engine - Docker Image
# Terminal-based game engine with GPU shaders

FROM python:3.11-slim

# Metadata
LABEL maintainer="ALT_LAS Team"
LABEL version="0.3.0"
LABEL description="Terminal-based game engine with GPU shaders"

# Environment
ENV PYTHONUNBUFFERED=1
ENV PYTHONDONTWRITEBYTECODE=1
ENV TERM=xterm-256color
ENV LANG=C.UTF-8

# Install system dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    # OpenGL support for GPU shaders
    libgl1-mesa-glx \
    libglu1-mesa \
    libgles2-mesa \
    libegl1 \
    # X11 for headless rendering
    xvfb \
    x11-utils \
    # Terminal support
    ncurses-term \
    # Build tools for Python packages
    gcc \
    g++ \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy requirements first for better caching
COPY requirements.txt .

# Install Python dependencies
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY . .

# Create necessary directories
RUN mkdir -p /app/saves /app/logs

# Set permissions
RUN chmod +x main.py

# Expose MCP server port (optional)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD python -c "import sys; sys.exit(0)"

# Default command - Run game
CMD ["python", "main.py"]

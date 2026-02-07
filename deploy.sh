#!/bin/bash

# Check if API Key is provided
if [ -z "$1" ]; then
    echo "Usage: ./deploy.sh <API_KEY>"
    echo "Example: ./deploy.sh adk_..."
    exit 1
fi

API_KEY=$1

echo "1. Logging in to Aedify registry..."
# Using --password-stdin for security so the key isn't in shell history
echo "$API_KEY" | docker login registry-01.aedify.ai -u rafaelm120403@gmail.com --password-stdin

if [ $? -ne 0 ]; then
    echo "Login failed."
    exit 1
fi

echo "2. Building image (linux/amd64)..."
# The --platform flag is crucial because you are on a generic Mac (likely ARM) 
# but the server needs Intel/AMD (x86_64)
docker build --platform linux/amd64 -t thryftaiserver:latest .

echo "3. Tagging image..."
docker tag thryftaiserver:latest registry-01.aedify.ai/thryftaiserver:latest

echo "4. Pushing image to Aedify..."
docker push registry-01.aedify.ai/thryftaiserver:latest

echo " Deployment code uploaded successfully!"

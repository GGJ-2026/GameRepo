#!/bin/bash
# Debug script for manual execution on the runner

IMAGE="unityci/editor:ubuntu-6000.3.5f1-webgl-3"
echo "Starting Debug Build..."
echo "Image: $IMAGE"

# 1. Ensure directories exist
mkdir -p build/WebGL
mkdir -p Library
chmod -R 777 build Library

# 2. Check License
if [ -d ./Unity_v6000.x.ulf ]; then
    echo "WARNING: ./Unity_v6000.x.ulf is a directory (Docker artifact). Removing it..."
    rm -rf ./Unity_v6000.x.ulf
fi

if [ ! -f ./Unity_v6000.x.ulf ]; then
    echo "ERROR: ./Unity_v6000.x.ulf not found in current directory."
    echo "Please paste your license content into this file before running."
    exit 1
fi

# 3. Run Docker
# We removed '-quit' to see if it stays alive. 
# You might need to Ctrl+C if it succeeds and hangs.
echo "Running Docker command..."

sudo docker run --rm \
  -v "$(pwd):/project" \
  -v "$(pwd)/Unity_v6000.x.ulf:/project/Unity_v6000.x.ulf" \
  -w /project \
  -e UNITY_LICENSE_FILE=/project/Unity_v6000.x.ulf \
  $IMAGE \
  unity-editor \
  -batchmode \
  -nographics \
  -noaudio \
  -manualLicenseFile /project/Unity_v6000.x.ulf \
  -projectPath . \
  -executeMethod Builder.BuildWebGL \
  -logFile /dev/stdout

echo "Docker command finished."

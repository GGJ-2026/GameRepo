#!/bin/bash
IMAGE="unityci/editor:ubuntu-6000.3.5f1-webgl-3"
echo "Starting Debug Build..."
echo "Image: $IMAGE"

mkdir -p build/WebGL
mkdir -p Library
chmod -R 777 build Library
if [ -d ./Unity_v6000.x.ulf ]; then
    echo "WARNING: ./Unity_v6000.x.ulf is a directory (Docker artifact). Removing it..."
    rm -rf ./Unity_v6000.x.ulf
fi

if [ ! -f ./Unity_v6000.x.ulf ]; then
    echo "ERROR: ./Unity_v6000.x.ulf not found in current directory."
    echo "Please paste your license content into this file before running."
    exit 1
fi

echo "Running Docker command..."

echo "Checking Unity Version..."
sudo docker run --rm --privileged $IMAGE unity-editor -version

echo "Starting Build..."
sudo docker run --rm --privileged \
  -v "$(pwd):/project" \
  -v "$(pwd)/Unity_v6000.x.ulf:/project/Unity_v6000.x.ulf" \
  -w /project \
  -e UNITY_LICENSE_FILE=/project/Unity_v6000.x.ulf \
  $IMAGE \
  unity-editor \
  -batchmode \
  -nographics \
  -noaudio \
  -quit \
  -manualLicenseFile /project/Unity_v6000.x.ulf \
  -projectPath . \
  -executeMethod Builder.BuildWebGL \
  -logFile /dev/stdout

echo "Docker command finished."
ls -R build || echo "Build directory empty/missing"

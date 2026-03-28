#!/usr/bin/env bash

# Sync script to move generated marketing screenshots to the marketing repository
# This script assumes it's being run from the Root of the Winnow.Client app:
# /home/jamesstubbington/projects/software/winnow-secure/winnow/winnow-core/src/Apps/Winnow.Client

set -e

# Define directories
SOURCE_DIR="./tests/screenshots/output"
TARGET_DIR="/home/jamesstubbington/projects/software/winnow-secure/winnow/winnow-marketing/src/assets"

# Create target dir if it doesn't exist (it should)
mkdir -p "$TARGET_DIR"

echo "🚀 Syncing marketing screenshots..."

if [ ! -d "$SOURCE_DIR" ]; then echo "❌ Error: Source directory $SOURCE_DIR not found. Run 'npm run screenshots' first."; exit 1; fi

# Copy PNG files (and WebP if we ever get it working)
cp "$SOURCE_DIR"/*.png "$SOURCE_DIR"/*.webp "$TARGET_DIR"/ 2>/dev/null || true

echo "✅ Successfully synced screenshots to $TARGET_DIR"
echo "👉 You can now commit and push the changes in the winnow-marketing repository."

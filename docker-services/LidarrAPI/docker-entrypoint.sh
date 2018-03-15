#!/bin/bash
set -e

# Entrypoint for the docker container

# First, we need to make sure the database is there
echo "[Entrypoint-LidarAPI] Running Database Migrations"
dotnet ef database update

#Second, start the Service
echo "[Entrypoint-LidarAPI] Starting LidarrAPI Service"
dotnet out/LidarrAPI.dll
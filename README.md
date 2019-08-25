# Lidarr Update Server 

[![Build Status](https://dev.azure.com/Lidarr/Lidarr/_apis/build/status/lidarr.LidarrAPI.Update?branchName=develop)](https://dev.azure.com/Lidarr/Lidarr/_build/latest?definitionId=2&branchName=develop)

This is the update API of [https://github.com/Lidarr/Lidarr](https://github.com/Lidarr/Lidarr). The API is forked from [Radarr's update server](https://github.com/Radarr/RadarrAPI.Update)

## Development

If you want to work on **LidarrAPI.Update**, make sure you have [.NET Core 2.1 SDK](https://www.microsoft.com/net/download/core) installed and [Visual Studio 2017 RC](https://www.visualstudio.com/vs/visual-studio-2017-rc/).

## Using Docker

If you would like to use the docker setup we have for this project, follow these directions:
- Setup Environment Variables
	- Make sure you set an environment variable PRIOR to running docker-compose up called `MYSQL_ROOT_PASSWORD` OR
	- Setup and .env file or another way of passing variables as documented here: [Docker Compose](https://docs.docker.com/compose/environment-variables/#the-env-file)
		
The most important thing is the `ApiKey`, the rest can be used **AS-IS**, but if the ApiKey is not set, fetching updates from AppVeyor and Github will not function correctly.

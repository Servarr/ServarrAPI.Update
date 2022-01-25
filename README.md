# Servarr Update Server

[![Build Status](https://dev.azure.com/Servarr/Servarr/_apis/build/status/Servarr.ServarrAPI.Update?branchName=master)](https://dev.azure.com/Servarr/Servarr/_build/latest?definitionId=5&branchName=master)

This is the update API of [Lidarr](https://github.com/Lidarr/Lidarr), [Prowlarr](https://github.com/Prowlarr/Prowlarr), [Radarr](https://github.com/Radarr/Radarr), and [Readarr](https://github.com/Readarr/Readarr).

## Development

If you want to work on **ServarrAPI.Update**, make sure you have [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) installed and [Visual Studio 2022](https://www.visualstudio.com/vs).

## Using Docker

If you would like to use the docker setup we have for this project, follow these directions:
- Setup Environment Variables
	- Make sure you set an environment variable PRIOR to running docker-compose up called `MYSQL_ROOT_PASSWORD` OR
	- Setup and .env file or another way of passing variables as documented here: [Docker Compose](https://docs.docker.com/compose/environment-variables/#the-env-file)
		
The most important thing is the `ApiKey`, the rest can be used **AS-IS**, but if the ApiKey is not set, fetching updates from AppVeyor and Github will not function correctly. The `Project` variable will define the github and azure project used (Radarr, Lidarr, or Readarr). The correct sentry DSN should also be set on deployment.

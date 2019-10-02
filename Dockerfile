FROM mcr.microsoft.com/dotnet/core/sdk:3.0-alpine AS sdk
WORKDIR /app

RUN dotnet tool install -g dotnet-aspnet-codegenerator

# copy everything else and build
COPY LidarrAPI/* ./

# Run needed things on build
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-alpine
WORKDIR /app
COPY --from=sdk /app/out/* ./

# Docker Entry
ENTRYPOINT ["dotnet", "LidarrAPI.dll"]

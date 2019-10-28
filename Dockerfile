FROM mcr.microsoft.com/dotnet/core/sdk:3.0-alpine AS sdk
WORKDIR /app
ARG config=Release

COPY src ./

RUN dotnet publish -c $config -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-alpine
WORKDIR /app
COPY --from=sdk /app/out/. ./

# Docker Entry
ENTRYPOINT ["dotnet", "LidarrAPI.dll"]

FROM mcr.microsoft.com/dotnet/sdk:5.0.201-focal AS sdk
WORKDIR /app
ARG config=Release

COPY src ./src

RUN dotnet publish -c $config --no-self-contained src/*.sln

FROM mcr.microsoft.com/dotnet/aspnet:5.0.4-focal
WORKDIR /app
COPY --from=sdk /app/_output/net5.0/publish/. ./

# Docker Entry
ENTRYPOINT ["dotnet", "ServarrAPI.dll"]

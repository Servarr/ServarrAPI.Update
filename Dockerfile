FROM mcr.microsoft.com/dotnet/core/sdk:3.1.301-bionic AS sdk
WORKDIR /app
ARG config=Release

COPY src ./

RUN dotnet publish -c $config --no-self-contained -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.5-bionic
WORKDIR /app
COPY --from=sdk /app/out/. ./

# Docker Entry
ENTRYPOINT ["dotnet", "ServarrAPI.dll"]

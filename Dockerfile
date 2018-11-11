FROM microsoft/dotnet:2.1-sdk
WORKDIR /app

RUN dotnet tool install -g dotnet-aspnet-codegenerator

# copy everything else and build
COPY LidarrAPI/* ./
COPY docker-services/LidarrAPI/docker-entrypoint.sh ./

# Windows screws with Line Endings, so do this to be 100% sure
RUN sed -i 's/\o015/\n/g' docker-entrypoint.sh

# Run needed things on build
RUN dotnet restore && dotnet publish -c Release -o out

# Docker Entry
ENTRYPOINT ["./docker-entrypoint.sh"]
FROM microsoft/dotnet:2.0-sdk
WORKDIR /app

# copy csproj and restore as distinct layers
COPY LidarrAPI/*.csproj ./
RUN dotnet restore

# copy everything else and build
COPY LidarrAPI/* ./
RUN dotnet publish -c Release -o out

ENTRYPOINT ["dotnet", "out/LidarrAPI.dll"]

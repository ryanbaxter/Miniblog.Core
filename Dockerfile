FROM mcr.microsoft.com/dotnet/core/sdk:2.1 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY src/*.csproj ./src/
RUN dotnet restore

# Copy everything else and build
COPY src/. ./src/
WORKDIR /app/src
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.1
ENV ASPNETCORE_URLS http://*:5000
WORKDIR /app
COPY --from=build /app/src/out ./
ENTRYPOINT ["dotnet", "Miniblog.Core.dll"]
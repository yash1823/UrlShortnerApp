# UrlShortnerApp/Dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY UrlShortnerApp/*.csproj ./UrlShortnerApp/
RUN dotnet restore

# Copy everything else and build
COPY UrlShortnerApp/. ./UrlShortnerApp/
WORKDIR /app/UrlShortnerApp
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/UrlShortnerApp/out ./

# Expose port 8080 for Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UrlShortnerApp.dll"]
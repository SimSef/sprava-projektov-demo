# Multi-stage Dockerfile for .NET 9 Blazor/ASP.NET app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy sources and restore/publish
COPY . .
RUN dotnet restore "SpravaProjektov/SpravaProjektov.csproj" \
  && dotnet publish "SpravaProjektov/SpravaProjektov.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Expose Kestrel on 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copy published app
COPY --from=build /app/publish .

# Point to the assembly
ENTRYPOINT ["dotnet", "SpravaProjektov.dll"]


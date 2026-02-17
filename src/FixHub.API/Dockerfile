# Build (context = repo root)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FixHub.API/FixHub.API.csproj", "src/FixHub.API/"]
COPY ["src/FixHub.Application/FixHub.Application.csproj", "src/FixHub.Application/"]
COPY ["src/FixHub.Domain/FixHub.Domain.csproj", "src/FixHub.Domain/"]
COPY ["src/FixHub.Infrastructure/FixHub.Infrastructure.csproj", "src/FixHub.Infrastructure/"]

RUN dotnet restore "src/FixHub.API/FixHub.API.csproj"
COPY src/ src/
RUN dotnet build "src/FixHub.API/FixHub.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "src/FixHub.API/FixHub.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FixHub.API.dll"]

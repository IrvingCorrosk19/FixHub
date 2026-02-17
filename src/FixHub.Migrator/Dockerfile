# Build (context = repo root)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FixHub.Migrator/FixHub.Migrator.csproj", "src/FixHub.Migrator/"]
COPY ["src/FixHub.Infrastructure/FixHub.Infrastructure.csproj", "src/FixHub.Infrastructure/"]
COPY ["src/FixHub.Application/FixHub.Application.csproj", "src/FixHub.Application/"]
COPY ["src/FixHub.Domain/FixHub.Domain.csproj", "src/FixHub.Domain/"]

RUN dotnet restore "src/FixHub.Migrator/FixHub.Migrator.csproj"
COPY src/ src/
RUN dotnet publish "src/FixHub.Migrator/FixHub.Migrator.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FixHub.Migrator.dll"]

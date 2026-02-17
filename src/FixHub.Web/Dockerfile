# Build (context = repo root)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FixHub.Web/FixHub.Web.csproj", "src/FixHub.Web/"]
RUN dotnet restore "src/FixHub.Web/FixHub.Web.csproj"
COPY src/ src/
RUN dotnet build "src/FixHub.Web/FixHub.Web.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "src/FixHub.Web/FixHub.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FixHub.Web.dll"]

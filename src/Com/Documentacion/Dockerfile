# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CarnetQRPlatform.sln .
COPY CarnetQRPlatform.Application/ CarnetQRPlatform.Application/
COPY CarnetQRPlatform.Domain/ CarnetQRPlatform.Domain/
COPY CarnetQRPlatform.Infrastructure/ CarnetQRPlatform.Infrastructure/
COPY CarnetQRPlatform.Web/ CarnetQRPlatform.Web/

RUN dotnet restore
RUN dotnet publish CarnetQRPlatform.Web/CarnetQRPlatform.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CarnetQRPlatform.Web.dll"]

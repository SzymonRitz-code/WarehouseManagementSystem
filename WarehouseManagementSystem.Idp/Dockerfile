FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src
COPY ["WarehouseManagementSystem.Idp/WarehouseManagementSystem.Idp.csproj", "WarehouseManagementSystem.Idp/"]
RUN dotnet restore "WarehouseManagementSystem.Idp/WarehouseManagementSystem.Idp.csproj"
COPY . .
WORKDIR "/src/WarehouseManagementSystem.Idp"
RUN dotnet publish "WarehouseManagementSystem.Idp.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends openssl \
    && mkdir -p /https \
    && openssl req -x509 -nodes -days 365 \
        -newkey rsa:2048 \
        -keyout /https/localhost.key \
        -out /https/localhost.crt \
        -subj "/CN=localhost" \
        -addext "subjectAltName=DNS:localhost,IP:127.0.0.1" \
    && openssl pkcs12 -export \
        -out /https/localhost.pfx \
        -inkey /https/localhost.key \
        -in /https/localhost.crt \
        -passout pass:wms-local-dev \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WarehouseManagementSystem.Idp.dll"]

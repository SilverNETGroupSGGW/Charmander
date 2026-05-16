FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Charmander.Api.csproj .
RUN dotnet restore Charmander.Api.csproj

COPY . .
RUN dotnet publish Charmander.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8000 \
    ASPNETCORE_ENVIRONMENT=Production \
    SERVICE_ACCOUNT_PATH=/app/serviceAccountKey.json \
    DATABASE_CONNECTION_STRING=Data Source=/app/data/devices.db

RUN mkdir -p /app/data

EXPOSE 8000
VOLUME ["/app/data"]

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Charmander.Api.dll"]

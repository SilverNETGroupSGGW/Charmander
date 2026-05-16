FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Charmander.Api.Api2.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish Charmander.Api.Api2.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8000 \
    SERVICE_ACCOUNT_PATH=/app/serviceAccountKey.json

EXPOSE 8000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Charmander.Api.dll"]

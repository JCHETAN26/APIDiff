# syntax=docker/dockerfile:1
# Build context is the repo root so the shared proto/ contracts are available
# to Grpc.Tools at build time (see docker-compose.yml / ADR 0005).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/api/src/ApiDiff.Api/ApiDiff.Api.csproj services/api/src/ApiDiff.Api/
RUN dotnet restore services/api/src/ApiDiff.Api/ApiDiff.Api.csproj
COPY proto/ proto/
COPY services/api/src/ services/api/src/
RUN dotnet publish services/api/src/ApiDiff.Api/ApiDiff.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ApiDiff.Api.dll"]

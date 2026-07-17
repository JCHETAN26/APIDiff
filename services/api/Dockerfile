# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/ApiDiff.Api/ApiDiff.Api.csproj src/ApiDiff.Api/
RUN dotnet restore src/ApiDiff.Api/ApiDiff.Api.csproj
COPY src/ ./src/
RUN dotnet publish src/ApiDiff.Api/ApiDiff.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ApiDiff.Api.dll"]

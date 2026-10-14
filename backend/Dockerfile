# Tüm .NET servisleri için tek Dockerfile. Hangi projenin yayınlanacağı derleme argümanıyla seçilir:
#   docker build -f backend/Dockerfile --build-arg PROJECT=src/Tezgah.Api --build-arg APP_DLL=Tezgah.Api.dll backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src
COPY nuget.config ./
COPY src/ src/
COPY tools/ tools/
RUN dotnet publish "$PROJECT" -c Release -o /app --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# tzdata: Europe/Istanbul gün sınırları için. curl: compose sağlık kontrolü için.
RUN apt-get update \
 && apt-get install -y --no-install-recommends tzdata curl \
 && rm -rf /var/lib/apt/lists/*
ARG APP_DLL
ENV APP_DLL=$APP_DLL \
    ASPNETCORE_HTTP_PORTS=8080 \
    TZ=UTC
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_DLL\" \"$@\"", "--"]

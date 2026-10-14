# Üç React uygulaması için tek Dockerfile; APP ile seçilir (storefront | seller | admin).
#   docker build -f frontend/Dockerfile --build-arg APP=storefront frontend
FROM node:22-slim AS build
ARG APP
# Tarayıcı API'ye bilgisayarın portundan ulaşır; container içindeki servis adından değil.
ARG VITE_API_URL=http://localhost:5080
WORKDIR /src
COPY package.json package-lock.json ./
COPY apps/storefront/package.json apps/storefront/
COPY apps/seller/package.json apps/seller/
COPY apps/admin/package.json apps/admin/
RUN npm ci --no-audit --no-fund
COPY apps/ apps/
RUN VITE_API_URL="$VITE_API_URL" npm run build -w "$APP"

FROM nginx:1.27-alpine
ARG APP
COPY nginx/spa.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/apps/${APP}/dist /usr/share/nginx/html
EXPOSE 80

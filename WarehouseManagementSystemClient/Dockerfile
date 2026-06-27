FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration docker

FROM nginx:1.27-alpine AS final
RUN apk add --no-cache openssl \
    && mkdir -p /etc/nginx/certs \
    && openssl req -x509 -nodes -days 365 \
        -newkey rsa:2048 \
        -keyout /etc/nginx/certs/localhost.key \
        -out /etc/nginx/certs/localhost.crt \
        -subj "/CN=localhost" \
        -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/WarehouseManagementSystemClient/browser /usr/share/nginx/html
EXPOSE 443

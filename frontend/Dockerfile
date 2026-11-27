FROM node:22.23.5-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build
FROM nginxinc/nginx-unprivileged:1.29-alpine
COPY --from=build /app/dist/metermind/browser /usr/share/nginx/html
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
ENV API_ORIGIN=http://api:8080
EXPOSE 8080

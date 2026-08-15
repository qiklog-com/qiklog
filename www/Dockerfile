# syntax=docker/dockerfile:1
FROM node:22-alpine AS build
ARG PUBLIC_APP_URL=http://localhost:5081
ENV PUBLIC_APP_URL=$PUBLIC_APP_URL
WORKDIR /src
COPY www/package.json www/package-lock.json ./
RUN npm ci
COPY www/ .
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /src/dist /usr/share/nginx/html
COPY www/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80

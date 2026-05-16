# ── Build stage ──────────────────────────────────────────────────────────────
FROM node:22-alpine AS build

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# VITE_BASE_API tells the frontend where the API lives.
# With nginx reverse-proxying /api/* → backend, /api is the right value.
ARG VITE_BASE_API=/api
ENV VITE_BASE_API=$VITE_BASE_API

RUN npm run build

# ── Serve stage ───────────────────────────────────────────────────────────────
FROM nginx:alpine

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

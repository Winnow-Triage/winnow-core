# 1. Build Stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package descriptors 
COPY package*.json ./

# Install dependencies strictly
RUN npm ci

# Copy full source and build
COPY . .

# Accept build arguments to bake into the static React bundle
ARG VITE_API_URL
ENV VITE_API_URL=${VITE_API_URL:-http://localhost:5000}
ENV NODE_OPTIONS="--max-old-space-size=4096"

RUN npm run build

# 2. Runtime Stage
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Remove default nginx static assets
RUN rm -rf ./*

# Copy compiled frontend from build stage
COPY --from=build /app/dist .

# Copy custom Nginx configuration file
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
ENTRYPOINT ["nginx", "-g", "daemon off;"]

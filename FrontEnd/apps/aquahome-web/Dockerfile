FROM node:22-alpine AS build
WORKDIR /app

# Copy workspace root first (npm workspaces needs this)
COPY package.json ./

# Copy shared package source
COPY packages/shared ./packages/shared

# Copy app source
COPY apps/aquahome-web ./apps/aquahome-web

# Install deps từ workspace root — links @fishlover/shared tự động
RUN npm install --workspace=apps/aquahome-web --include-workspace-root

# Vite inlines VITE_* tại build time — phải pass qua ARG
ARG VITE_GATEWAY_URL
ARG VITE_AUTH_CLIENT_ID
ARG VITE_AUTH_REDIRECT_URI
ARG VITE_AUTH_POST_LOGOUT_URI
ARG VITE_APP_VERSION=unknown

ENV VITE_GATEWAY_URL=$VITE_GATEWAY_URL
ENV VITE_AUTH_CLIENT_ID=$VITE_AUTH_CLIENT_ID
ENV VITE_AUTH_REDIRECT_URI=$VITE_AUTH_REDIRECT_URI
ENV VITE_AUTH_POST_LOGOUT_URI=$VITE_AUTH_POST_LOGOUT_URI
ENV VITE_APP_VERSION=$VITE_APP_VERSION

WORKDIR /app/apps/aquahome-web
RUN npm run build

# ── Serve static với Nginx ─────────────────────────────────
FROM nginx:alpine AS final
COPY --from=build /app/apps/aquahome-web/dist /usr/share/nginx/html
COPY apps/aquahome-web/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

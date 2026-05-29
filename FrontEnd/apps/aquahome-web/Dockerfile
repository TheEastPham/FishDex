FROM node:20-alpine AS build
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
ARG VITE_AQUAHOME_API_URL
ARG VITE_FISHDEX_API_URL
ARG VITE_AUTH_ISSUER
ARG VITE_AUTH_CLIENT_ID
ARG VITE_AUTH_REDIRECT_URI
ARG VITE_AUTH_POST_LOGOUT_URI

ENV VITE_AQUAHOME_API_URL=$VITE_AQUAHOME_API_URL
ENV VITE_FISHDEX_API_URL=$VITE_FISHDEX_API_URL
ENV VITE_AUTH_ISSUER=$VITE_AUTH_ISSUER
ENV VITE_AUTH_CLIENT_ID=$VITE_AUTH_CLIENT_ID
ENV VITE_AUTH_REDIRECT_URI=$VITE_AUTH_REDIRECT_URI
ENV VITE_AUTH_POST_LOGOUT_URI=$VITE_AUTH_POST_LOGOUT_URI

WORKDIR /app/apps/aquahome-web
RUN npm run build

# ── Serve static với Nginx ─────────────────────────────────
FROM nginx:alpine AS final
COPY --from=build /app/apps/aquahome-web/dist /usr/share/nginx/html
COPY apps/aquahome-web/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

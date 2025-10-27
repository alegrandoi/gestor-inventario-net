# Etapa 1: instalación de dependencias
FROM node:20-alpine AS deps
WORKDIR /app
COPY src/Presentation/gestor-inventario-web/package.json ./
COPY src/Presentation/gestor-inventario-web/package-lock.json ./
RUN npm ci

# Etapa 2: build de la aplicación
FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY src/Presentation/gestor-inventario-web ./
RUN npm run build

# Etapa 3: imagen lista para producción
FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
RUN apk add --no-cache curl \
    && addgroup -S appgroup \
    && adduser -S appuser -G appgroup
COPY --from=builder /app/next.config.mjs ./next.config.mjs
COPY --from=builder /app/public ./public
COPY --from=builder /app/package.json ./package.json
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
RUN chown -R appuser:appgroup /app
USER appuser
EXPOSE 3000
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 CMD curl --fail http://127.0.0.1:${PORT}/ || exit 1
CMD ["node", "server.js"]

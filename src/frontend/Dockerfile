# ══════════════════════════════════════════════════════════════════════════════
# SIMS Web-Frontend — static SPA served by nginx, with /api reverse-proxied
# to the backend container. No build step required.
# ══════════════════════════════════════════════════════════════════════════════
FROM nginx:1.27-alpine

# Custom server config (static files + /api proxy to the API container)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Static single-page application
COPY index.html styles.css app.js /usr/share/nginx/html/

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD wget -qO- http://localhost/ > /dev/null 2>&1 || exit 1

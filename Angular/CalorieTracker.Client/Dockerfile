# Etapa 1: Construcción (Node.js)
FROM node:20-alpine AS build
WORKDIR /app

# Copiar dependencias e instalarlas (aprovechando caché de capas de Docker)
COPY package*.json ./
RUN npm ci

# Copiar el código fuente y compilar la PWA para producción
COPY . .
RUN npm run build --configuration=production

# Etapa 2: Servidor Web (Nginx ligero)
FROM nginx:alpine
# Remover la página por defecto de Nginx
RUN rm -rf /usr/share/nginx/html/*

# Copiar nuestra configuración personalizada
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copiar los artefactos de construcción de Angular
# NOTA: En Angular 17+, la ruta de salida por defecto incluye "/browser". 
# Ajusta "calorietracker.client" si el nombre en tu angular.json es distinto.
COPY --from=build /app/dist/calorietracker.client/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
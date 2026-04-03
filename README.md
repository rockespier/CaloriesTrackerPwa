# 🥗 CaloriesTrackerPwa

Una **Progressive Web App (PWA)** para el seguimiento de calorías diarias con análisis nutricional impulsado por Inteligencia Artificial.

---

## 📋 Tabla de Contenidos

- [Descripción](#-descripción)
- [Características](#-características)
- [Arquitectura](#-arquitectura)
- [Tecnologías](#-tecnologías)
- [Requisitos Previos](#-requisitos-previos)
- [Instalación y Configuración](#-instalación-y-configuración)
- [Variables de Entorno](#-variables-de-entorno)
- [Endpoints de la API](#-endpoints-de-la-api)
- [Ejecución de Tests](#-ejecución-de-tests)
- [Docker](#-docker)
- [CI/CD](#-cicd)
- [Estado del Proyecto](#-estado-del-proyecto)

---

## 📖 Descripción

**CaloriesTrackerPwa** es una aplicación web progresiva que permite a los usuarios registrar y monitorear su ingesta calórica diaria. Usando la API de **Google Gemini**, la aplicación analiza descripciones de comidas en lenguaje natural y estima automáticamente las calorías consumidas. Además, calcula el objetivo calórico personalizado de cada usuario utilizando la **fórmula de Harris-Benedict**.

---

## ✨ Características

- 🔐 **Autenticación segura** — Registro e inicio de sesión con JWT (tokens Bearer)
- 👤 **Perfil de usuario personalizable** — Altura, peso, edad, sexo, nivel de actividad y objetivo de peso
- 🧮 **Cálculo calórico automático** — Calcula el TDEE (Total Daily Energy Expenditure) con ajustes según el objetivo (pérdida/ganancia de peso ±500 kcal)
- 🤖 **IA nutricional (Google Gemini)** — Escribe "pollo con arroz y ensalada" y la IA estima las calorías por ti
- 📊 **Historial y estadísticas** — Consulta los registros por día y estadísticas en rangos de fechas
- 🔒 **Gestión segura de secretos** — Integración con Azure Key Vault para entornos de producción
- 📱 **PWA** — Diseñada para funcionar en dispositivos móviles y de escritorio

---

## 🏗️ Arquitectura

El proyecto sigue los principios de **Clean Architecture** con separación clara de responsabilidades:

```
CaloriesTrackerPwa/
├── CalorieTracker.Api/              # Capa de presentación: Minimal APIs (.NET 9)
├── CalorieTracker.Application/      # Casos de uso, comandos e interfaces
├── CalorieTracker.Domain/           # Entidades y lógica de negocio
├── CalorieTracker.Infrastructure/   # EF Core, repositorios, servicios externos
├── CalorieTracker.Tests/            # Tests unitarios (xUnit + Moq)
└── Angular/
    └── CalorieTracker.Client/       # Frontend Angular PWA (en desarrollo)
```

### Capas

| Capa | Responsabilidad |
|------|----------------|
| **Domain** | Entidades (`User`, `FoodLog`, `UserProfileHistory`), enums y reglas de negocio |
| **Application** | Casos de uso (`RegisterUser`, `Login`, `LogFood`, `UpdateProfile`), interfaces e interfaces de repositorios |
| **Infrastructure** | Implementaciones: EF Core DbContext, repositorios SQL, JWT, Gemini API |
| **API** | Configuración de DI, endpoints, middleware de autenticación y CORS |

---

## 🛠️ Tecnologías

### Backend
| Tecnología | Versión |
|-----------|---------|
| .NET / ASP.NET Core | 9.0 |
| Entity Framework Core | 9.0 |
| SQL Server | 2022 |
| JWT Bearer Auth | IdentityModel 8.0 |
| Google Gemini API | 2.5 Flash |
| Azure Key Vault | SDK |
| xUnit | 2.9.2 |
| Moq | 4.20.72 |

### Frontend
| Tecnología | Versión |
|-----------|---------|
| Angular | PWA |
| Node.js | 20 |

### DevOps
- Docker & Docker Compose
- GitHub Actions

---

## 🔧 Requisitos Previos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) y Docker Compose
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (o usar el contenedor incluido)
- Clave de API de [Google Gemini](https://aistudio.google.com/)

---

## 🚀 Instalación y Configuración

### 1. Clonar el repositorio

```bash
git clone https://github.com/rockespier/CaloriesTrackerPwa.git
cd CaloriesTrackerPwa
```

### 2. Configurar variables de entorno

Crea o edita `CalorieTracker.Api/appsettings.Development.json` con tus valores (ver sección [Variables de Entorno](#-variables-de-entorno)).

### 3. Aplicar migraciones de base de datos

```bash
dotnet ef database update --project CalorieTracker.Infrastructure \
  --startup-project CalorieTracker.Api
```

### 4. Ejecutar el backend

```bash
dotnet run --project CalorieTracker.Api
```

La API estará disponible en `http://localhost:8080`.

### 5. Ejecutar el frontend (Angular)

```bash
cd Angular/CalorieTracker.Client
npm install
npm start
```

El frontend estará disponible en `http://localhost:4200`.

---

## 🌐 Variables de Entorno

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `JwtSettings__Secret` | Clave secreta para firmar los tokens JWT | `mi-secreto-muy-seguro` |
| `JwtSettings__Issuer` | Emisor del token JWT | `CalorieTrackerApi` |
| `JwtSettings__Audience` | Audiencia del token JWT | `CalorieTrackerPwa` |
| `JwtSettings__ExpiryMinutes` | Tiempo de expiración del token (minutos) | `60` |
| `Gemini__ApiKey` | Clave de API de Google Gemini | `AIza...` |
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a SQL Server | `Server=...` |
| `Azure__KeyVault__Uri` | URI del Azure Key Vault (solo producción) | `https://kv-...` |

> ⚠️ **Nunca** incluyas secretos reales en el código fuente ni en los archivos de configuración versionados.

---

## 📡 Endpoints de la API

Todos los endpoints (excepto registro y login) requieren el header `Authorization: Bearer <token>`.

### Autenticación

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/v1/users/register` | Registrar un nuevo usuario |
| `POST` | `/v1/users/login` | Iniciar sesión (retorna JWT) |

### Perfil de Usuario

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/v1/users/profile` | Obtener el perfil del usuario autenticado |
| `PUT` | `/v1/users/profile` | Actualizar el perfil del usuario |

### Nutrición

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/v1/nutrition/log` | Registrar un alimento (la IA estima las calorías) |
| `GET` | `/v1/nutrition/history/{date}` | Obtener registros de una fecha específica |
| `GET` | `/v1/nutrition/stats` | Estadísticas nutricionales en un rango de fechas |

### Dashboard

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/v1/dashboard/summary` | Resumen del panel principal |

---

## ✅ Ejecución de Tests

```bash
dotnet test CalorieTracker.Tests/CalorieTracker.Tests.csproj
```

Los tests cubren:
- Registro de usuarios (`RegisterUserUseCaseTests`)
- Inicio de sesión (`LoginUseCaseTests`)
- Registro de alimentos (`LogFoodUseCaseTests`)
- Lógica de dominio (`UserTests`)

---

## 🐳 Docker

Levanta todos los servicios (SQL Server, API y Frontend) con un solo comando:

```bash
docker-compose up --build
```

| Servicio | Puerto |
|----------|--------|
| SQL Server 2022 | `1433` |
| Backend API | `8080` |
| Frontend PWA | `80` |

Para detener los contenedores:

```bash
docker-compose down
```

---

## ⚙️ CI/CD

El pipeline de **GitHub Actions** (`.github/workflows/ci.yml`) se ejecuta automáticamente en cada `push` o `pull request` a `main` y realiza los siguientes pasos:

1. **Build Backend**: Restaura dependencias y compila el proyecto .NET en modo `Release`.
2. **Build Frontend**: Instala dependencias Node.js y genera el build de producción de Angular.

---

## 📌 Estado del Proyecto

| Componente | Estado |
|-----------|--------|
| Backend (.NET 9 API) | ✅ Implementado |
| Base de datos (EF Core + SQL Server) | ✅ Configurado |
| Autenticación JWT | ✅ Implementado |
| Integración con Google Gemini | ✅ Implementado |
| Tests unitarios | ✅ Implementado |
| Docker / Docker Compose | ✅ Configurado |
| Frontend Angular PWA | 🚧 En desarrollo |

---

## 📄 Licencia

Este proyecto está bajo la licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más detalles.

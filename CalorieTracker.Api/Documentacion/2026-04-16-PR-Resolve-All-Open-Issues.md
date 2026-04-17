# Pull Request: Resolve 10 Open Issues

**Branch:** `feature/resolve-all-open-issues`  
**Base:** `main`  
**Date:** 2026-04-16

## Para crear el PR

```bash
git push origin feature/resolve-all-open-issues
# Luego abrir en GitHub y crear el PR desde esa rama
```

---

## Descripción

Este PR implementa las soluciones de los **10 issues abiertos** (ramas `copilot/*`) en un único commit limpio y revisado.

---

## Cambios incluidos

### 🗑️ `copilot/remove-empty-nutrition-controller`
- Eliminado `CalorieTracker.Api/Controllers/NutritionController.cs` — controlador vacío sin ninguna función real que generaba ruido en el proyecto.

### 🔒 `copilot/secure-database-credentials`
- Eliminadas las credenciales reales de base de datos de `appsettings.Development.json`.
- Reemplazadas por una connection string genérica a `localhost` para desarrollo local.

### 🔒 `copilot/fix-jwt-secret-placeholder`
- `appsettings.json`: reemplazado el placeholder de 26 chars por uno de ≥32 chars con instrucciones claras.
- `Program.cs`: añadida validación al inicio (`InvalidOperationException`) si el secreto JWT es nulo o tiene menos de 32 caracteres — el secreto debe provenir de `dotnet user-secrets`, variables de entorno o Azure Key Vault.

### ✨ `copilot/add-global-exception-handler`
- Nuevo: `CalorieTracker.Api/Middleware/GlobalExceptionHandler.cs` — implementa `IExceptionHandler` (nativo .NET 9).
- Mapea `UnauthorizedAccessException` → 401, `InvalidOperationException` → 409, `ArgumentException` → 400, resto → 500.
- Registrado en `Program.cs` con `AddExceptionHandler<GlobalExceptionHandler>()` y `app.UseExceptionHandler()`.
- Simplificados los handlers de `/register` y `/login` (los try/catch redundantes son ahora gestionados globalmente).

### 📖 `copilot/add-swagger-documentation`
- Añadido `Scalar.AspNetCore` (v2.4.5) al proyecto API.
- `Program.cs`: configurado `AddOpenApi("v1")` con metadatos y `BearerSecuritySchemeTransformer`.
- Rutas `/openapi/v1.json` y `/scalar/v1` disponibles en entornos no productivos.
- Endpoints `/register` y `/login` decorados con `.WithSummary()` y `.WithDescription()`.

### 🔒 `copilot/add-validation-to-commands`
- Nuevo: `CalorieTracker.Api/Filters/ValidationFilter<T>.cs` — endpoint filter genérico que valida DataAnnotations antes de ejecutar el handler.
- `LoginCommand`: añadidos `[Required]`, `[EmailAddress]`, `[StringLength]`.
- `RegisterUserCommand`: añadidos `[Required]`, `[Range]`, `[RegularExpression]` en todos los campos.
- Filtro registrado en `/register` y `/login` con `.AddEndpointFilter<ValidationFilter<T>>()`.

### ⚡ `copilot/add-indexes-userid-loggedat`
- `CalorieTrackerDbContext`: añadidos índices compuestos `IX_FoodLogs_UserId_LoggedAt` y `IX_UserProfileHistory_UserId_RecordedAt` via Fluent API.
- Nueva migración: `20260416200000_AddPerformanceIndexes.cs`.

### ♻️ `copilot/refactor-caloric-calculation-logic`
- Nuevo: `CalorieTracker.Domain/Services/CaloricCalculatorService.cs` — servicio de dominio estático con la fórmula Mifflin-St Jeor centralizada.
- Expone `CalculateBMR`, `CalculateTDEE`, `CalculateDailyTarget` y constantes `GoalLose/GoalGain/GoalMaintain`.

### ♻️ `copilot/refactor-userservice-and-goal-enum`
- Nuevo: `CalorieTracker.Domain/Entities/UserGoal.cs` — enum tipado (`Lose`, `Maintain`, `Gain`) que reemplaza magic strings.

### 🧪 `copilot/add-tests-for-critical-components`
- `CalorieTracker.Tests.csproj`: añadidas referencias a `Infrastructure` y `Microsoft.EntityFrameworkCore.InMemory`.
- Nuevos tests:
  - `Tests/Infrastructure/JwtTokenGeneratorTests.cs` — 5 tests (happy + sad path)
  - `Tests/Infrastructure/NutritionRepositoryTests.cs` — 4 tests con InMemory DB
  - `Tests/Infrastructure/UserServiceTests.cs` — 2 tests (happy + sad path)
  - `Tests/Application/GetWeeklyStatusUseCaseTests.cs` — 3 tests
  - `Tests/Domain/CaloricCalculatorServiceTests.cs` — 7 tests (BMR, TDEE, objetivos)

---

## Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `CalorieTracker.Api/Controllers/NutritionController.cs` | ❌ Eliminado |
| `CalorieTracker.Api/Middleware/GlobalExceptionHandler.cs` | ✅ Nuevo |
| `CalorieTracker.Api/Filters/ValidationFilter.cs` | ✅ Nuevo |
| `CalorieTracker.Api/Program.cs` | ✏️ Modificado |
| `CalorieTracker.Api/CalorieTracker.Api.csproj` | ✏️ Añadido Scalar |
| `CalorieTracker.Api/appsettings.json` | ✏️ Placeholder JWT seguro |
| `CalorieTracker.Api/appsettings.Development.json` | ✏️ Sin credenciales reales |
| `CalorieTracker.Application/Commands/LoginCommand.cs` | ✏️ DataAnnotations |
| `CalorieTracker.Application/Commands/RegisterUserCommand.cs` | ✏️ DataAnnotations |
| `CalorieTracker.Domain/Entities/UserGoal.cs` | ✅ Nuevo |
| `CalorieTracker.Domain/Services/CaloricCalculatorService.cs` | ✅ Nuevo |
| `CalorieTracker.Infrastructure/Data/CalorieTrackerDbContext.cs` | ✏️ Índices compuestos |
| `CalorieTracker.Infrastructure/Migrations/20260416200000_AddPerformanceIndexes.cs` | ✅ Nuevo |
| `CalorieTracker.Tests/CalorieTracker.Tests.csproj` | ✏️ Referencias Infrastructure + EFCore.InMemory |
| `CalorieTracker.Tests/Application/GetWeeklyStatusUseCaseTests.cs` | ✅ Nuevo |
| `CalorieTracker.Tests/Domain/CaloricCalculatorServiceTests.cs` | ✅ Nuevo |
| `CalorieTracker.Tests/Infrastructure/JwtTokenGeneratorTests.cs` | ✅ Nuevo |
| `CalorieTracker.Tests/Infrastructure/NutritionRepositoryTests.cs` | ✅ Nuevo |
| `CalorieTracker.Tests/Infrastructure/UserServiceTests.cs` | ✅ Nuevo |

using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Application.Services;
using CalorieTracker.Application.UseCases;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Auth;
using CalorieTracker.Infrastructure.Data;
using CalorieTracker.Infrastructure.Repositories;
using CalorieTracker.Infrastructure.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configurare CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPwa", policy =>
    {
        policy.WithOrigins("http://localhost:51363") // La tua PWA
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configurare Azure Key Vault (solo in produzione o quando configurato)
if (!string.IsNullOrEmpty(builder.Configuration["Azure:KeyVault:Uri"]))
{
    var keyVaultUri = new Uri(builder.Configuration["Azure:KeyVault:Uri"]!);

    // Opzione 1: Con Client ID e Client Secret (per ambienti non-Azure)
    var clientId = builder.Configuration["Azure:KeyVault:ClientId"];
    var clientSecret = builder.Configuration["Azure:KeyVault:ClientSecret"];
    var tenantId = builder.Configuration["Azure:KeyVault:TenantId"];

    if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(tenantId))
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        builder.Configuration.AddAzureKeyVault(keyVaultUri, credential);
    }
    else
    {
        // Opzione 2: Managed Identity (per Azure App Service, Azure Functions, ecc.)
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
}

// 1. Configuraci�n de Base de Datos
builder.Services.AddDbContext<CalorieTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Inyecci�n de Dependencias (IoC)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<RegisterUserUseCase>();

// 3. Inyectar Generador de JWT y Caso de Uso
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<LoginUseCase>();


//builder.Services.AddScoped<INutritionAnalyzer, LocalPatternNutritionAnalyzer>();
builder.Services.AddHttpClient<INutritionAnalyzer, GeminiNutritionAnalyzer>(client =>
{
    // Configuraci�n de resiliencia b�sica: Timeout aumentado a 30 segundos para manejar latencia de la API de Gemini
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IFoodLogRepository, FoodLogRepository>();
builder.Services.AddScoped<INutritionRepository, NutritionRepository>();
builder.Services.AddScoped<LogFoodUseCase>();

builder.Services.AddScoped<UserService>();

// Actividad física con Gemini
builder.Services.AddHttpClient<IActivityAnalyzer, GeminiActivityAnalyzer>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<LogActivityUseCase>();

// 4. Configurar Autenticaci�n JWT nativa
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 3. Activar Middleware CORS (deve essere prima di UseAuthentication)
app.UseCors("AllowPwa");

// 3. Activar Middleware
app.UseAuthentication();
app.UseAuthorization();

// Dopo app.UseAuthorization(); e prima dei gruppi di endpoint

app.MapGet("/", () => Results.Ok(new
{
    Message = "Calorie Tracker API",
    Version = "1.0",
    Status = "Running"
}))
.WithName("Root")
.AllowAnonymous();

var nutritionGroup = app.MapGroup("/v1/nutrition")
    .WithTags("Nutrition")
    .RequireAuthorization(); // Requiere JWT v�lido

nutritionGroup.MapPost("/log", async (HttpContext context, LogFoodRequest request, LogFoodUseCase useCase, ILogger<Program> logger) =>
{
    try
    {
        // Extraer UserId del token JWT (Claim: sub)
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Results.Unauthorized();
        }

        var command = new LogFoodCommand(userId, request.Text);
        var caloriesAdded = await useCase.ExecuteAsync(command);

        return Results.Ok(new { Calories = caloriesAdded, Message = "Alimento registrado exitosamente." });
    }
    catch (ApplicationException ex) when (ex.InnerException is TaskCanceledException || ex.Message.Contains("tiempo"))
    {
        logger.LogWarning(ex, "Timeout al procesar el alimento para el usuario. Descripci�n: {Text}", request.Text);
        return Results.Json(
            new { Message = "El servicio est� tardando m�s de lo esperado. Por favor, intenta de nuevo en unos momentos." },
            statusCode: StatusCodes.Status504GatewayTimeout
        );
    }
    catch (ApplicationException ex)
    {
        logger.LogError(ex, "Error de aplicaci�n al procesar el alimento. Mensaje: {Message}", ex.Message);
        return Results.Json(
            new { Message = ex.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error inesperado al procesar el alimento. Mensaje: {Message}", ex.Message);
        return Results.Problem("Error interno al procesar el alimento. Por favor, intenta de nuevo m�s tarde.");
    }
})
.WithName("LogFood");

// 5. Definici�n de Endpoints (Minimal API)
var usersGroup = app.MapGroup("/v1/users").WithTags("Users");

usersGroup.MapPost("/register", async (RegisterUserCommand command, RegisterUserUseCase useCase) =>
{
    try
    {
        // En un entorno real de .NET 9, aqu� usar�amos Endpoint Filters para validar el Command
        // antes de que llegue al Use Case (Ej: DataAnnotations nativos).

        var userId = await useCase.ExecuteAsync(command);

        // Retornamos 201 Created cumpliendo con los est�ndares REST
        return Results.Created($"/v1/users/{userId}", new { Id = userId });
    }
    catch (InvalidOperationException ex)
    {
        // 409 Conflict es el c�digo HTTP sem�nticamente correcto cuando un recurso (email) ya existe.
        return Results.Conflict(new { Message = ex.Message });
    }
    catch (Exception)
    {
        // 500 Internal Server Error protegido (sin exponer el stack trace al cliente)
        return Results.Problem("Ha ocurrido un error inesperado al procesar el registro.");
    }
})
.WithName("RegisterUser")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status409Conflict)
.Produces(StatusCodes.Status500InternalServerError);

usersGroup.MapPost("/login", async (LoginCommand command, LoginUseCase useCase) =>
{
    try
    {
        var token = await useCase.ExecuteAsync(command);
        return Results.Ok(new { Token = token });
    }
    catch (UnauthorizedAccessException)
    {
        // 401 Unauthorized para credenciales inv�lidas. Nunca confirmar si el error fue el correo o la contrase�a por seguridad.
        return Results.Unauthorized();
    }
    catch (Exception)
    {
        return Results.Problem("Ha ocurrido un error inesperado durante la autenticaci�n.");
    }
})
.WithName("LoginUser")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);

// Endpoint: GET /api/v1/users/profile
usersGroup.MapGet("/profile", async (HttpContext context, CalorieTrackerDbContext db) =>
{
    var userId = GetUserIdFromClaims(context); // El helper que ya tenemos
    var user = await db.Users
        .Where(u => u.Id == userId)
        .Select(u => new {
            u.CurrentWeightKg,
            u.HeightCm,
            u.Age,
            u.BiologicalSex,
            u.ActivityLevel,
            u.Goal,
            u.DailyCaloricTarget,
            u.TargetWeightKg
        })
        .FirstOrDefaultAsync();

    return user is not null ? Results.Ok(user) : Results.NotFound();
})
.RequireAuthorization();

// Endpoint: PUT /api/v1/users/profile
usersGroup.MapPut("/profile", async (UpdateProfileCommand dto, HttpContext context, UserService userService) =>
{
    var userId = GetUserIdFromClaims(context);
    var success = await userService.UpdateProfileAsync(userId, dto);

    return success
        ? Results.Ok(new { message = "Perfil actualizado con �xito" })
        : Results.BadRequest("No se pudo actualizar el perfil");
})
.RequireAuthorization();

// Endpoint de prueba seguro
var dashboardGroup = app.MapGroup("/v1/dashboard").WithTags("Dashboard").RequireAuthorization();

dashboardGroup.MapGet("/summary", () =>
{
    return Results.Ok(new { Message = "Acceso autorizado al resumen nutricional." });
});

// Endpoint para obtener los logs de un d�a espec�fico
// Endpoint para obtener los logs de un d�a espec�fico
nutritionGroup.MapGet("/history/{date}", async (string date, HttpContext context, [FromServices] INutritionRepository repo) =>
{
    var userId = GetUserIdFromClaims(context);

    // Usar formato invariante y manejar varios formatos de fecha
    if (!DateTime.TryParseExact(date, new[] { "yyyy-MM-dd", "dd-MM-yyyy", "MM-dd-yyyy" },
        System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.None,
        out DateTime parsedDate))
    {
        return Results.BadRequest(new { Message = "Fecha inv�lida. Use el formato yyyy-MM-dd (ej: 2026-04-02)" });
    }

    var logs = await repo.GetLogsByDateAsync(userId, parsedDate);
    var logsList = logs.ToList(); // Materializar para evitar m�ltiples enumeraciones
    var total = logsList.Sum(l => l.EstimatedCalories);

    return Results.Ok(new
    {
        Date = parsedDate.ToString("yyyy-MM-dd"),
        Logs = logsList,
        TotalCalories = total
    });
})
.WithName("GetHistoryByDate");

// Endpoint para obtener el total de calor�as de un d�a espec�fico
nutritionGroup.MapGet("/daily-total", async (string? date, HttpContext context, [FromServices] INutritionRepository repo) =>
{
    var userId = GetUserIdFromClaims(context);
    
    DateTime targetDate;
    
    // Si no se proporciona fecha, usar hoy
    if (string.IsNullOrEmpty(date))
    {
        targetDate = DateTime.UtcNow.Date;
    }
    else
    {
        if (!DateTime.TryParseExact(date, new[] { "yyyy-MM-dd", "dd-MM-yyyy", "MM-dd-yyyy" },
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out targetDate))
        {
            return Results.BadRequest(new { Message = "Fecha inv�lida. Use el formato yyyy-MM-dd (ej: 2026-04-02)" });
        }
    }

    var totalCalories = await repo.GetTotalCaloriesForDateAsync(userId, targetDate);

    return Results.Ok(new
    {
        Date = targetDate.ToString("yyyy-MM-dd"),
        TotalCalories = totalCalories
    });
})
.WithName("GetDailyTotal");

// Endpoint para obtener el resumen semanal
nutritionGroup.MapGet("/weekly-summary", async (HttpContext context, [FromServices] INutritionRepository repo, [FromServices] CalorieTrackerDbContext db) =>
{
    var userId = GetUserIdFromClaims(context);
    
    // Obtener los �ltimos 7 d�as
    var endDate = DateTime.UtcNow.Date;
    var startDate = endDate.AddDays(-6); // 7 d�as incluyendo hoy
    
    var stats = await repo.GetStatsInRangeAsync(userId, startDate, endDate);
    var statsList = stats.ToList();
    
    // Obtener el objetivo cal�rico del usuario
    var user = await db.Users.FindAsync(userId);
    var dailyTarget = user?.DailyCaloricTarget ?? 0;
    
    // Calcular promedios y totales
    var totalCalories = statsList.Sum(s => (int)s.GetType().GetProperty("TotalCalories")!.GetValue(s)!);
    var averageCalories = statsList.Any() ? totalCalories / 7 : 0; // Dividir por 7 d�as
    
    return Results.Ok(new
    {
        StartDate = startDate.ToString("yyyy-MM-dd"),
        EndDate = endDate.ToString("yyyy-MM-dd"),
        DailyStats = statsList,
        TotalCalories = totalCalories,
        AverageCalories = averageCalories,
        DailyTarget = dailyTarget,
        AverageDifference = averageCalories - dailyTarget
    });
})
.WithName("GetWeeklySummary");

// Endpoint para estad�sticas de rango (Dashboard/Gr�ficos)
nutritionGroup.MapGet("/stats", async (DateTime startDate, DateTime endDate, HttpContext context, [FromServices] INutritionRepository repo) =>
{
    var userId = GetUserIdFromClaims(context);
    var stats = await repo.GetStatsInRangeAsync(userId, startDate, endDate);
    return Results.Ok(stats);
})
.WithName("GetNutritionStats");




// ─── Endpoints de Actividad Física ───────────────────────────────────────────
var activityGroup = app.MapGroup("/v1/activity")
    .WithTags("Activity")
    .RequireAuthorization();

// POST /v1/activity/log — Registrar actividad y calcular calorías quemadas
activityGroup.MapPost("/log", async (HttpContext context, LogActivityRequest request, LogActivityUseCase useCase, ILogger<Program> logger) =>
{
    try
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            return Results.Unauthorized();

        if (request.DurationMinutes <= 0)
            return Results.BadRequest(new { Message = "La duración debe ser mayor a cero minutos." });

        var command = new LogActivityCommand(userId, request.ActivityDescription, request.DurationMinutes);
        var caloriesBurned = await useCase.ExecuteAsync(command);

        return Results.Ok(new
        {
            CaloriesBurned = caloriesBurned,
            Message = $"Actividad registrada: {caloriesBurned} kcal quemadas."
        });
    }
    catch (ApplicationException ex) when (ex.Message.Contains("no disponible"))
    {
        logger.LogWarning(ex, "Servicio Gemini no disponible para actividad: {Activity}", request.ActivityDescription);
        return Results.Json(
            new { Message = "El servicio está tardando más de lo esperado. Por favor, intenta de nuevo." },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al registrar actividad: {Activity}", request.ActivityDescription);
        return Results.Problem("Error interno al procesar la actividad. Por favor, intenta de nuevo.");
    }
})
.WithName("LogActivity");

// GET /v1/activity/history/{date} — Historial de actividades de un día
activityGroup.MapGet("/history/{date}", async (string date, HttpContext context, [FromServices] IActivityLogRepository repo) =>
{
    var userId = GetUserIdFromClaims(context);

    if (!DateTime.TryParseExact(date, new[] { "yyyy-MM-dd", "dd-MM-yyyy", "MM-dd-yyyy" },
        CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.None,
        out DateTime parsedDate))
    {
        return Results.BadRequest(new { Message = "Fecha inválida. Use el formato yyyy-MM-dd (ej: 2026-04-16)" });
    }

    var logs = await repo.GetByUserAndDateAsync(userId, parsedDate);
    var logsList = logs.ToList();
    var totalBurned = logsList.Sum(l => l.CaloriesBurned);

    return Results.Ok(new
    {
        Date = parsedDate.ToString("yyyy-MM-dd"),
        Activities = logsList.Select(l => new
        {
            l.Id,
            l.ActivityDescription,
            l.DurationMinutes,
            l.CaloriesBurned,
            l.LoggedAt
        }),
        TotalCaloriesBurned = totalBurned
    });
})
.WithName("GetActivityHistory");

// GET /v1/activity/daily-burned?date=yyyy-MM-dd — Total quemado en el día
activityGroup.MapGet("/daily-burned", async (string? date, HttpContext context, [FromServices] IActivityLogRepository repo) =>
{
    var userId = GetUserIdFromClaims(context);

    DateTime targetDate;
    if (string.IsNullOrEmpty(date))
    {
        targetDate = DateTime.UtcNow.Date;
    }
    else
    {
        if (!DateTime.TryParseExact(date, new[] { "yyyy-MM-dd", "dd-MM-yyyy", "MM-dd-yyyy" },
            CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out targetDate))
        {
            return Results.BadRequest(new { Message = "Fecha inválida. Use el formato yyyy-MM-dd" });
        }
    }

    var totalBurned = await repo.GetTotalBurnedByUserAndDateAsync(userId, targetDate);

    return Results.Ok(new
    {
        Date = targetDate.ToString("yyyy-MM-dd"),
        TotalCaloriesBurned = totalBurned
    });
})
.WithName("GetDailyBurned");

// Helper local para extraer el Guid del usuario autenticado
Guid GetUserIdFromClaims(HttpContext context)
{
    var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
    {
        // Lanzamos una excepci�n que nuestro GlobalExceptionHandler atrapar� y convertir� en un 401/500 seguro
        throw new UnauthorizedAccessException("Token inv�lido o ID de usuario no encontrado.");
    }

    return userId;
}

app.Run();

// DTO para la petici�n HTTP
public record LogFoodRequest(string Text);
public record LogActivityRequest(string ActivityDescription, int DurationMinutes);



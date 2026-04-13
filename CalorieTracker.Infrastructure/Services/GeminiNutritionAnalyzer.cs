using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CalorieTracker.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.Infrastructure.Services
{
    public class GeminiNutritionAnalyzer(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiNutritionAnalyzer> logger) : INutritionAnalyzer
    {
        // Modelos de Gemini ordenados por preferencia (de más rápido/avanzado a más básico)
        private static readonly string[] FallbackModels = 
        [
            "gemini-2.5-flash",      // Modelo principal: baja latencia, última generación
            "gemini-1.5-flash",      // Fallback 1: estable, ampliamente disponible
            "gemini-1.5-flash-8b"    // Fallback 2: más ligero, mayor disponibilidad
        ];

        public async Task<int> AnalyzeCaloriesAsync(string foodDescription)
        {
            var apiKey = configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini API Key is missing in configuration.");

            // Ingeniería de Prompt estructurada para garantizar la integridad de los datos
            var prompt = $@"Actúa como un nutricionista clínico experto.
Estima las calorías totales del siguiente consumo de alimentos: '{foodDescription}'.
Reglas de procesamiento estricto:
1. Si el usuario no especifica gramos o cantidades, asume una porción estándar promedio (ej. 1 taza de arroz cocido, 150g de proteína, 1 rebanada de pan).
2. Debes calcular la suma total de las calorías de todos los elementos mencionados.
3. CRÍTICO: Tu respuesta debe contener ÚNICAMENTE el número entero del total de calorías. No incluyas texto, ni explicaciones, ni la palabra 'kcal', ni rangos. Solo el dígito numérico.";

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            // Intentar con cada modelo en cascada
            List<Exception> attemptErrors = [];
            
            foreach (var model in FallbackModels)
            {
                try
                {
                    var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                    
                    logger.LogInformation("Intentando análisis nutricional con el modelo: {Model}", model);
                    
                    var response = await httpClient.PostAsJsonAsync(endpoint, payload);
                    
                    // Si recibimos 503 (Service Unavailable), intentar con el siguiente modelo
                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        logger.LogWarning("Modelo {Model} no disponible (503). Intentando con modelo de respaldo...", model);
                        attemptErrors.Add(new HttpRequestException($"Modelo {model} retornó 503 Service Unavailable"));
                        continue; // Pasar al siguiente modelo
                    }
                    
                    // Para otros errores HTTP, lanzar excepción
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

                    // Navegación segura por el árbol JSON de la respuesta de Gemini
                    var responseText = jsonResponse
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString()?.Trim();

                    if (int.TryParse(responseText, out int calories))
                    {
                        if (model != FallbackModels[0])
                        {
                            logger.LogInformation("Análisis exitoso con modelo de respaldo: {Model}. Calorías estimadas: {Calories}", model, calories);
                        }
                        return calories;
                    }

                    logger.LogWarning("El modelo {Model} devolvió un formato no numérico: {ResponseText}. No se pudieron extraer las calorías.", model, responseText);
                    attemptErrors.Add(new InvalidOperationException($"Modelo {model} devolvió respuesta no numérica: {responseText}"));
                    continue; // Intentar con el siguiente modelo
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    logger.LogError(ex, "Timeout al comunicarse con el modelo {Model}. La solicitud excedió el tiempo límite configurado.", model);
                    attemptErrors.Add(ex);
                    continue; // Intentar con el siguiente modelo
                }
                catch (TaskCanceledException ex)
                {
                    logger.LogError(ex, "La solicitud al modelo {Model} fue cancelada.", model);
                    attemptErrors.Add(ex);
                    continue; // Intentar con el siguiente modelo
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(ex, "Error de red al comunicarse con el modelo {Model}.", model);
                    attemptErrors.Add(ex);
                    
                    // Si no es un 503, no intentar más modelos
                    if (!ex.Message.Contains("503"))
                    {
                        break;
                    }
                    continue; // Intentar con el siguiente modelo
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error inesperado procesando la respuesta del modelo {Model}.", model);
                    attemptErrors.Add(ex);
                    continue; // Intentar con el siguiente modelo
                }
            }

            // Si llegamos aquí, todos los modelos fallaron
            logger.LogError("Todos los modelos de Gemini fallaron. Total de intentos: {AttemptCount}. Errores: {Errors}", 
                attemptErrors.Count, 
                string.Join(" | ", attemptErrors.Select(e => e.Message)));

            throw new ApplicationException(
                "Servicio de análisis nutricional temporalmente no disponible. Todos los modelos de respaldo fallaron.",
                attemptErrors.FirstOrDefault());
        }
    }
}
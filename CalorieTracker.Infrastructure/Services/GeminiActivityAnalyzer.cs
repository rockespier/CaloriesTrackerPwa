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
    public class GeminiActivityAnalyzer(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiActivityAnalyzer> logger) : IActivityAnalyzer
    {
        // Mismos modelos de fallback que el analizador nutricional
        private static readonly string[] FallbackModels =
        [
            "gemini-2.5-flash",
            "gemini-1.5-flash",
            "gemini-1.5-flash-8b"
        ];

        public async Task<int> AnalyzeCaloriesBurnedAsync(
            string activityDescription,
            int durationMinutes,
            double weightKg)
        {
            var apiKey = configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini API Key is missing in configuration.");

            var prompt = $@"Actúa como un fisiólogo del ejercicio experto.
Estima las calorías totales quemadas por una persona de {weightKg:F1} kg que realizó la siguiente actividad física: '{activityDescription}' durante {durationMinutes} minutos.
Reglas de procesamiento estricto:
1. Considera el tipo de actividad, la intensidad típica y el peso corporal indicado.
2. Si la actividad es ambigua (ej: 'ejercicio', 'deporte'), asume una intensidad moderada típica.
3. Incluye el gasto metabólico basal durante el ejercicio en el cálculo.
4. CRÍTICO: Tu respuesta debe contener ÚNICAMENTE el número entero de las calorías quemadas. No incluyas texto, ni explicaciones, ni la palabra 'kcal', ni rangos. Solo el dígito numérico.";

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            List<Exception> attemptErrors = [];

            foreach (var model in FallbackModels)
            {
                try
                {
                    var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                    logger.LogInformation("Analizando actividad física con el modelo: {Model}", model);

                    var response = await httpClient.PostAsJsonAsync(endpoint, payload);

                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        logger.LogWarning("Modelo {Model} no disponible (503). Intentando con modelo de respaldo...", model);
                        attemptErrors.Add(new HttpRequestException($"Modelo {model} retornó 503 Service Unavailable"));
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

                    var responseText = jsonResponse
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString()?.Trim();

                    if (int.TryParse(responseText, out int caloriesBurned))
                    {
                        if (model != FallbackModels[0])
                        {
                            logger.LogInformation("Análisis de actividad exitoso con modelo de respaldo: {Model}. Calorías quemadas: {Calories}", model, caloriesBurned);
                        }
                        return caloriesBurned;
                    }

                    logger.LogWarning("El modelo {Model} devolvió un formato no numérico: {ResponseText}.", model, responseText);
                    attemptErrors.Add(new InvalidOperationException($"Modelo {model} devolvió respuesta no numérica: {responseText}"));
                    continue;
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    logger.LogError(ex, "Timeout al comunicarse con el modelo {Model}.", model);
                    attemptErrors.Add(ex);
                    continue;
                }
                catch (TaskCanceledException ex)
                {
                    logger.LogError(ex, "La solicitud al modelo {Model} fue cancelada.", model);
                    attemptErrors.Add(ex);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(ex, "Error de red al comunicarse con el modelo {Model}.", model);
                    attemptErrors.Add(ex);

                    if (!ex.Message.Contains("503"))
                        break;

                    continue;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error inesperado procesando respuesta del modelo {Model}.", model);
                    attemptErrors.Add(ex);
                    continue;
                }
            }

            logger.LogError("Todos los modelos de Gemini fallaron para análisis de actividad. Errores: {Errors}",
                string.Join(" | ", attemptErrors.Select(e => e.Message)));

            throw new ApplicationException(
                "Servicio de análisis de actividad física temporalmente no disponible. Todos los modelos de respaldo fallaron.",
                attemptErrors.FirstOrDefault());
        }
    }
}

using System;
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
        public async Task<int> AnalyzeCaloriesAsync(string foodDescription)
        {
            var apiKey = configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini API Key is missing in configuration.");

            // Utilizamos el modelo Flash por su baja latencia, ideal para respuestas en tiempo real en la PWA
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

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

            try
            {
                var response = await httpClient.PostAsJsonAsync(endpoint, payload);
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
                    return calories;
                }

                logger.LogWarning("El modelo de IA devolvió un formato no numérico: {ResponseText}. No se pudieron extraer las calorías.", responseText);
                throw new InvalidOperationException("La IA no pudo determinar las calorías con exactitud.");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error de red al comunicarse con la API de Gemini.");
                throw new ApplicationException("Servicio de análisis nutricional temporalmente no disponible.", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inesperado procesando la respuesta de la IA.");
                throw new ApplicationException("Error al procesar el alimento.", ex);
            }
        }
    }
}
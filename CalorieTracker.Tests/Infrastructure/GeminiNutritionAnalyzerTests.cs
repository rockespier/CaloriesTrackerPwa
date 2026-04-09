using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CalorieTracker.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CalorieTracker.Tests.Infrastructure
{
    public class GeminiNutritionAnalyzerTests
    {
        private static IConfiguration BuildConfig(string? apiKey = "test-api-key")
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["Gemini:ApiKey"] = apiKey
                })
                .Build();
        }

        private static string BuildGeminiResponse(string text)
        {
            var payload = new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[] { new { text } }
                        }
                    }
                }
            };
            return JsonSerializer.Serialize(payload);
        }

        private static HttpClient BuildHttpClient(HttpMessageHandler handler) => new(handler);

        [Fact]
        public async Task AnalyzeCaloriesAsync_HappyPath_ValidResponse_ReturnsCalories()
        {
            // Arrange
            var json = BuildGeminiResponse("350");
            var handler = new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var logger = new Mock<ILogger<GeminiNutritionAnalyzer>>();
            var analyzer = new GeminiNutritionAnalyzer(BuildHttpClient(handler), BuildConfig(), logger.Object);

            // Act
            var result = await analyzer.AnalyzeCaloriesAsync("pollo a la plancha");

            // Assert
            Assert.Equal(350, result);
        }

        [Fact]
        public async Task AnalyzeCaloriesAsync_SadPath_HttpRequestException_ThrowsApplicationException()
        {
            // Arrange
            var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
            var logger = new Mock<ILogger<GeminiNutritionAnalyzer>>();
            var analyzer = new GeminiNutritionAnalyzer(BuildHttpClient(handler), BuildConfig(), logger.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(
                () => analyzer.AnalyzeCaloriesAsync("pollo"));

            Assert.Contains("temporalmente no disponible", ex.Message);
        }

        [Fact]
        public async Task AnalyzeCaloriesAsync_SadPath_Timeout_ThrowsApplicationException()
        {
            // Arrange
            var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("Timeout"));
            var logger = new Mock<ILogger<GeminiNutritionAnalyzer>>();
            var analyzer = new GeminiNutritionAnalyzer(BuildHttpClient(handler), BuildConfig(), logger.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(
                () => analyzer.AnalyzeCaloriesAsync("pollo"));

            Assert.Contains("Error al procesar", ex.Message);
        }

        [Fact]
        public async Task AnalyzeCaloriesAsync_SadPath_NonNumericResponse_ThrowsApplicationException()
        {
            // Arrange — the model returns a non-numeric string
            var json = BuildGeminiResponse("No puedo calcular calorías.");
            var handler = new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var logger = new Mock<ILogger<GeminiNutritionAnalyzer>>();
            var analyzer = new GeminiNutritionAnalyzer(BuildHttpClient(handler), BuildConfig(), logger.Object);

            // Act & Assert
            // InvalidOperationException is caught by the catch(Exception) block → ApplicationException
            var ex = await Assert.ThrowsAsync<ApplicationException>(
                () => analyzer.AnalyzeCaloriesAsync("alimento desconocido"));

            Assert.Contains("Error al procesar", ex.Message);
        }

        [Fact]
        public async Task AnalyzeCaloriesAsync_SadPath_ApiKeyMissing_ThrowsInvalidOperationException()
        {
            // Arrange — config without an API key
            var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var logger = new Mock<ILogger<GeminiNutritionAnalyzer>>();
            var analyzer = new GeminiNutritionAnalyzer(
                BuildHttpClient(handler),
                BuildConfig(apiKey: null),
                logger.Object);

            // Act & Assert
            // InvalidOperationException is caught by catch(Exception) → ApplicationException
            var ex = await Assert.ThrowsAsync<ApplicationException>(
                () => analyzer.AnalyzeCaloriesAsync("pollo"));

            Assert.Contains("Error al procesar", ex.Message);
        }

        [Fact]
        public async Task AnalyzeCaloriesAsync_SadPath_HttpErrorStatus_ThrowsApplicationException()
        {
            // Arrange — server returns 500 Internal Server Error
            var handler = new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var logger = new Mock<ILogger<GeminiNutritionAnalyzer>>();
            var analyzer = new GeminiNutritionAnalyzer(BuildHttpClient(handler), BuildConfig(), logger.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(
                () => analyzer.AnalyzeCaloriesAsync("pollo"));

            Assert.NotNull(ex);
        }

        // ─── Test doubles ────────────────────────────────────────────────────────

        private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(response);
            }
        }

        private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromException<HttpResponseMessage>(exception);
        }
    }
}

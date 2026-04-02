using System;
using System.Threading.Tasks;
using CalorieTracker.Application.Interfaces;

namespace CalorieTracker.Infrastructure.Services
{
    public class LocalPatternNutritionAnalyzer : INutritionAnalyzer
    {
        public Task<int> AnalyzeCaloriesAsync(string foodDescription)
        {
            // Implementación simulada (Mock). En producción, esto llamaría a un modelo NLP.
            var lowerText = foodDescription.ToLowerInvariant();
            int estimatedCalories = 0;

            if (lowerText.Contains("huevo")) estimatedCalories += 70;
            if (lowerText.Contains("pan")) estimatedCalories += 80;
            if (lowerText.Contains("manzana")) estimatedCalories += 52;
            if (lowerText.Contains("pollo")) estimatedCalories += 165;
            if (lowerText.Contains("arroz")) estimatedCalories += 130;

            // Multiplicador básico si detecta palabras de plural o cantidad (muy rudimentario, solo para MVP)
            if (lowerText.Contains("dos") || lowerText.Contains("2"))
            {
                estimatedCalories *= 2;
            }

            // Fallback si no reconoce nada
            if (estimatedCalories == 0) estimatedCalories = 100;

            return Task.FromResult(estimatedCalories);
        }
    }
}
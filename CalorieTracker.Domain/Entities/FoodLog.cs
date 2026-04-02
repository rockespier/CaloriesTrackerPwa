using System;

namespace CalorieTracker.Domain.Entities
{
    public class FoodLog
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string OriginalText { get; private set; }
        public int EstimatedCalories { get; private set; }
        public DateTime LoggedAt { get; private set; }

        private FoodLog() { } // Requerido por EF Core

        public FoodLog(Guid userId, string originalText, int estimatedCalories)
        {
            if (userId == Guid.Empty) throw new ArgumentException("El ID de usuario es inválido.");
            if (string.IsNullOrWhiteSpace(originalText)) throw new ArgumentException("El texto del alimento no puede estar vacío.");
            if (estimatedCalories < 0) throw new ArgumentException("Las calorías no pueden ser negativas.");

            Id = Guid.NewGuid();
            UserId = userId;
            OriginalText = originalText;
            EstimatedCalories = estimatedCalories;
            LoggedAt = DateTime.UtcNow;
        }
    }
}
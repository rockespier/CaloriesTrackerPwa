using System;

namespace CalorieTracker.Domain.Entities
{
    public class ActivityLog
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string ActivityDescription { get; private set; }
        public int DurationMinutes { get; private set; }
        public int CaloriesBurned { get; private set; }
        public DateTime LoggedAt { get; private set; }

        private ActivityLog() { } // Requerido por EF Core

        public ActivityLog(Guid userId, string activityDescription, int durationMinutes, int caloriesBurned)
        {
            if (userId == Guid.Empty) throw new ArgumentException("El ID de usuario es inválido.");
            if (string.IsNullOrWhiteSpace(activityDescription)) throw new ArgumentException("La descripción de la actividad no puede estar vacía.");
            if (durationMinutes <= 0) throw new ArgumentException("La duración debe ser mayor a cero minutos.");
            if (caloriesBurned < 0) throw new ArgumentException("Las calorías quemadas no pueden ser negativas.");

            Id = Guid.NewGuid();
            UserId = userId;
            ActivityDescription = activityDescription;
            DurationMinutes = durationMinutes;
            CaloriesBurned = caloriesBurned;
            LoggedAt = DateTime.UtcNow;
        }
    }
}

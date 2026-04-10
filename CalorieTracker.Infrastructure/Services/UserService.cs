using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Data;
using CalorieTracker.Application.Commands;
using Microsoft.EntityFrameworkCore;

namespace CalorieTracker.Application.Services
{
    public class UserService(CalorieTrackerDbContext context)
    {
        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileCommand dto )
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null) return false;

            // 1. CREAR REGISTRO HISTÓRICO (Antes de modificar el actual)
            // Esto permite ver la "Transformación" en el tiempo
            var historyEntry = new UserProfileHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Weight = user.CurrentWeightKg, // Guardamos el peso que TENÍA
                Height = user.HeightCm,
                ActivityLevel = user.ActivityLevel,
                RecordedAt = DateTime.UtcNow
            };

            context.UserProfileHistory.Add(historyEntry);

            // 2. ACTUALIZAR DATOS ACTUALES DEL USUARIO
            user.CurrentWeightKg = dto.weight;
            user.HeightCm = dto.height;
            user.Age = dto.age;
            user.BiologicalSex = dto.gender;
            user.ActivityLevel = dto.activityLevel;
            user.Goal = dto.goal;

            // Recalculamos la meta calórica basado en los nuevos datos
            user.DailyCaloricTarget = CalculateTarget(user);

            return await context.SaveChangesAsync() > 0;
        }

        private int CalculateTarget(User user)
        {
            // Fórmula Mifflin-St Jeor simplificada
            double bmr = (10 * user.CurrentWeightKg) + (6.25 * user.HeightCm) - (5 * user.Age);
            bmr = (user.BiologicalSex == 'M') ? bmr + 5 : bmr - 161;

            double factor = user.ActivityLevel switch
            {
                ActivityLevel.Sedentary => 1.2,
                ActivityLevel.LightlyActive => 1.375,
                ActivityLevel.ModeratelyActive => 1.55,
                ActivityLevel.VeryActive => 1.725,
                ActivityLevel.ExtraActive => 1.9,
                _ => 1.2
            };

            double tdee = bmr * factor;

            // Ajuste según objetivo
            return user.Goal switch
            {
                "Perder" => (int)(tdee - 500),
                "Ganar" => (int)(tdee + 300),
                _ => (int)tdee
            };
        }
    }
}
using CalorieTracker.Domain.Entities;
using CalorieTracker.Domain.Services;
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
            user.CurrentWeightKg = dto.Weight;
            user.HeightCm = dto.Height;
            user.Age = dto.Age;
            user.BiologicalSex = dto.Gender;
            user.ActivityLevel = dto.ActivityLevel;
            user.Goal = dto.Goal;

            // Recalculamos la meta calórica basado en los nuevos datos
            user.DailyCaloricTarget = CalculateTarget(user);

            return await context.SaveChangesAsync() > 0;
        }

        private static int CalculateTarget(User user)
        {
            double bmr = CaloricCalculatorService.CalculateBMR(user.CurrentWeightKg, user.HeightCm, user.Age, user.BiologicalSex);
            double tdee = CaloricCalculatorService.CalculateTDEE(bmr, user.ActivityLevel);
            return (int)CaloricCalculatorService.CalculateDailyTarget(tdee, user.Goal);
        }
    }
}
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.Services
{
    public class UserService(IUserRepository userRepository)
    {
        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileCommand dto)
        {
            var user = await userRepository.GetByIdAsync(userId);
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

            await userRepository.AddProfileHistoryAsync(historyEntry);

            // 2. ACTUALIZAR DATOS ACTUALES DEL USUARIO
            user.CurrentWeightKg = dto.Weight;
            user.HeightCm = dto.Height;
            user.Age = dto.Age;
            user.BiologicalSex = dto.Gender;
            user.ActivityLevel = dto.ActivityLevel;
            user.Goal = dto.Goal;

            // Recalculamos la meta calórica basado en los nuevos datos
            user.DailyCaloricTarget = CalculateTarget(user);

            await userRepository.UpdateAsync(user);
            return true;
        }

        private static int CalculateTarget(User user)
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
            double adjustment = user.Goal switch
            {
                UserGoal.Lose => -500,
                UserGoal.Gain => +300,
                UserGoal.Maintain => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(user.Goal), user.Goal, null)
            };

            return (int)(tdee + adjustment);
        }
    }
}

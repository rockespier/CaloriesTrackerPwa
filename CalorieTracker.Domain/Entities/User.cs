using System;

namespace CalorieTracker.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string Name { get; private set; }
        public double HeightCm { get; private set; }
        public double CurrentWeightKg { get; private set; }
        public double TargetWeightKg { get; private set; }
        public int Age { get; private set; }
        public char BiologicalSex { get; private set; } // 'M' o 'F'
        public ActivityLevel ActivityLevel { get; private set; }

        private User() { } // Para EF Core

        public User(string email, string passwordHash, string name, double heightCm, double currentWeightKg, double targetWeightKg, int age, char biologicalSex, ActivityLevel activityLevel)
        {
            // Validaciones de dominio (Guard Clauses) omitidas por brevedad, pero obligatorias en producción.
            Id = Guid.NewGuid();
            Email = email;
            PasswordHash = passwordHash;
            Name = name;
            HeightCm = heightCm;
            CurrentWeightKg = currentWeightKg;
            TargetWeightKg = targetWeightKg;
            Age = age;
            BiologicalSex = biologicalSex;
            ActivityLevel = activityLevel;
        }

        public double CalculateDailyCaloricTarget()
        {
            double bmr = BiologicalSex == 'M'
                ? 88.362 + (13.397 * CurrentWeightKg) + (4.799 * HeightCm) - (5.677 * Age)
                : 447.593 + (9.247 * CurrentWeightKg) + (3.098 * HeightCm) - (4.330 * Age);

            double tdee = bmr * GetActivityMultiplier();

            // Lógica simple de déficit: Si el peso objetivo es menor, reducimos 500 kcal para un déficit saludable (aprox 0.5kg por semana).
            if (TargetWeightKg < CurrentWeightKg)
            {
                return tdee - 500;
            }
            if (TargetWeightKg > CurrentWeightKg)
            {
                return tdee + 500;
            }

            return tdee; // Mantenimiento
        }

        private double GetActivityMultiplier()
        {
            return ActivityLevel switch
            {
                ActivityLevel.Sedentary => 1.2,
                ActivityLevel.LightlyActive => 1.375,
                ActivityLevel.ModeratelyActive => 1.55,
                ActivityLevel.VeryActive => 1.725,
                ActivityLevel.ExtraActive => 1.9,
                _ => 1.2
            };
        }
    }

    public enum ActivityLevel
    {
        Sedentary,
        LightlyActive,
        ModeratelyActive,
        VeryActive,
        ExtraActive
    }
}

using System;

namespace CalorieTracker.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string Name { get; set; }
        public double HeightCm { get; set; }
        public double CurrentWeightKg { get; set; }
        public double TargetWeightKg { get; set; }
        public int Age { get; set; }
        public char BiologicalSex { get; set; } // 'M' o 'F'
        public ActivityLevel ActivityLevel { get; set; }
        public int DailyCaloricTarget { get; set; }
        public string Goal { get; set; }

        private User() { } // Para EF Core

        public User(string email, string passwordHash, string name, double heightCm, double currentWeightKg, double targetWeightKg, int age, char biologicalSex, ActivityLevel activityLevel, string goal = "Mantener")
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
            Goal = goal;
            
            // Calcular el objetivo calórico al crear el usuario
            DailyCaloricTarget = (int)CalculateDailyCaloricTarget();
        }

        public double CalculateDailyCaloricTarget()
        {
            double bmr = BiologicalSex == 'M'
                ? 88.362 + (13.397 * CurrentWeightKg) + (4.799 * HeightCm) - (5.677 * Age)
                : 447.593 + (9.247 * CurrentWeightKg) + (3.098 * HeightCm) - (4.330 * Age);

            double tdee = bmr * GetActivityMultiplier();

            // Lógica basada en el Goal del usuario
            return Goal switch
            {
                "Perder" => tdee - 500,
                "Ganar" => tdee + 300,
                _ => tdee // Mantener
            };
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

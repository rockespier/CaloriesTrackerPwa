using System;
using CalorieTracker.Domain.Services;

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
            double bmr = CaloricCalculatorService.CalculateBMR(CurrentWeightKg, HeightCm, Age, BiologicalSex);
            double tdee = CaloricCalculatorService.CalculateTDEE(bmr, ActivityLevel);

            // Derive goal from weight targets when the Goal string property is not set.
            string? effectiveGoal = Goal
                ?? (TargetWeightKg < CurrentWeightKg ? CaloricCalculatorService.GoalLose
                    : TargetWeightKg > CurrentWeightKg ? CaloricCalculatorService.GoalGain
                    : null);

            return CaloricCalculatorService.CalculateDailyTarget(tdee, effectiveGoal);
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

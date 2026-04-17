using CalorieTracker.Application.DTOs;
using CalorieTracker.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalorieTracker.Application.UseCases
{
    public class GetWeeklyStatusUseCase(INutritionRepository repository, IUserRepository userRepository)
    {
        public async Task<WeeklyStatusDto> ExecuteAsync(Guid userId)
        {
            var user = await userRepository.GetByIdAsync(userId) 
                ?? throw new InvalidOperationException($"Usuario con ID {userId} no encontrado.");
            
            var dailyTarget = user.DailyCaloricTarget;
            var history = await repository.GetDailyHistoryAsync(userId, 7);

            // Lógica de cumplimiento: Comparar consumo real vs meta acumulada
            double totalConsumed = 0;
            int daysLogged = 0;

            foreach (var day in history)
            {
                totalConsumed += day.TotalCalories;
                daysLogged++;
            }

            var weeklyTarget = dailyTarget * 7;
            var status = totalConsumed <= (dailyTarget * daysLogged) ? "En Meta" : "Exceso";

            return new WeeklyStatusDto(
                DailyTarget: dailyTarget,
                WeeklyConsumed: totalConsumed,
                WeeklyTarget: weeklyTarget,
                ComplianceStatus: status,
                DaysActive: daysLogged
            );
        }
    }
}

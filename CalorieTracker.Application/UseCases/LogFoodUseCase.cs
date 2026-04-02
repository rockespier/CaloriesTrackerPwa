using System;
using System.Threading.Tasks;
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.UseCases
{
    public class LogFoodUseCase
    {
        private readonly IFoodLogRepository _repository;
        private readonly INutritionAnalyzer _analyzer;

        public LogFoodUseCase(IFoodLogRepository repository, INutritionAnalyzer analyzer)
        {
            _repository = repository;
            _analyzer = analyzer;
        }

        public async Task<int> ExecuteAsync(LogFoodCommand command)
        {
            int calories = await _analyzer.AnalyzeCaloriesAsync(command.FoodText);

            var foodLog = new FoodLog(command.UserId, command.FoodText, calories);
            await _repository.AddAsync(foodLog);

            return calories;
        }
    }
}
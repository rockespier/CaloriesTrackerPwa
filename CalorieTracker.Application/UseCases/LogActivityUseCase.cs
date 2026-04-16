using System;
using System.Threading.Tasks;
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.UseCases
{
    public class LogActivityUseCase
    {
        private readonly IActivityLogRepository _repository;
        private readonly IActivityAnalyzer _analyzer;
        private readonly IUserRepository _userRepository;

        public LogActivityUseCase(
            IActivityLogRepository repository,
            IActivityAnalyzer analyzer,
            IUserRepository userRepository)
        {
            _repository = repository;
            _analyzer = analyzer;
            _userRepository = userRepository;
        }

        public async Task<int> ExecuteAsync(LogActivityCommand command)
        {
            // Obtener el usuario para usar su peso en el cálculo de calorías quemadas
            var user = await _userRepository.GetByIdAsync(command.UserId)
                ?? throw new InvalidOperationException("Usuario no encontrado.");

            int caloriesBurned = await _analyzer.AnalyzeCaloriesBurnedAsync(
                command.ActivityDescription,
                command.DurationMinutes,
                user.CurrentWeightKg);

            var activityLog = new ActivityLog(
                command.UserId,
                command.ActivityDescription,
                command.DurationMinutes,
                caloriesBurned);

            await _repository.AddAsync(activityLog);

            return caloriesBurned;
        }
    }
}

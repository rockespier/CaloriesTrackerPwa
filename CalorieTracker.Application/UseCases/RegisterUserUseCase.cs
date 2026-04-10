using System;
using System.Threading.Tasks;
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CalorieTracker.Application.UseCases
{
    public class RegisterUserUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;

        public RegisterUserUseCase(IUserRepository userRepository, IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> ExecuteAsync(RegisterUserCommand command)
        {
            if (await _userRepository.ExistsByEmailAsync(command.Email))
            {
                throw new InvalidOperationException("El correo electrónico ya está registrado.");
            }

            // Instanciamos el usuario temporalmente para pasar al Hasher. 
            // En implementaciones nativas de Identity, el hasher usa la entidad para salt o configuraciones.
            var dummyUser = new User(command.Email, string.Empty, command.Name, command.HeightCm, command.CurrentWeightKg, command.TargetWeightKg, command.Age, command.BiologicalSex, command.ActivityLevel, command.Goal);

            string hashedPassword = _passwordHasher.HashPassword(dummyUser, command.Password);

            var user = new User(
                command.Email,
                hashedPassword,
                command.Name,
                command.HeightCm,
                command.CurrentWeightKg,
                command.TargetWeightKg,
                command.Age,
                command.BiologicalSex,
                command.ActivityLevel,
                command.Goal
            );

            await _userRepository.AddAsync(user);

            return user.Id;
        }
    }
}

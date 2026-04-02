using System;
using System.Threading.Tasks;
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CalorieTracker.Application.UseCases
{
    public class LoginUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LoginUseCase(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<string> ExecuteAsync(LoginCommand command)
        {
            var user = await _userRepository.GetByEmailAsync(command.Email);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Credenciales inválidas.");
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedAccessException("Credenciales inválidas.");
            }

            return _jwtTokenGenerator.GenerateToken(user);
        }
    }
}
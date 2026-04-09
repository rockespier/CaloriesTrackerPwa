using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CalorieTracker.Application.Commands;
using CalorieTracker.Domain.Entities;
using Xunit;

namespace CalorieTracker.Tests.Application
{
    public class ValidationCommandTests
    {
        private static IList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        // ──── LoginCommand ────────────────────────────────────────────────

        [Fact]
        public void LoginCommand_HappyPath_NoValidationErrors()
        {
            var command = new LoginCommand("user@example.com", "SecurePass1");
            var errors = Validate(command);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("", "Password1")]          // email vacío
        [InlineData("not-an-email", "Password1")] // email malformado
        [InlineData("user@example.com", "")]   // password vacío
        [InlineData("user@example.com", "short")] // password demasiado corta (<8)
        public void LoginCommand_SadPath_InvalidInput_ReturnsValidationErrors(string email, string password)
        {
            var command = new LoginCommand(email, password);
            var errors = Validate(command);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void LoginCommand_SadPath_EmailTooLong_ReturnsValidationError()
        {
            var longEmail = new string('a', 250) + "@x.com"; // >255 chars
            var command = new LoginCommand(longEmail, "Password1");
            var errors = Validate(command);
            Assert.NotEmpty(errors);
        }

        // ──── RegisterUserCommand ─────────────────────────────────────────

        [Fact]
        public void RegisterUserCommand_HappyPath_NoValidationErrors()
        {
            var command = new RegisterUserCommand(
                "juan@example.com", "SecurePass1!", "Juan",
                175, 80, 75, 28, 'M', ActivityLevel.ModeratelyActive);
            var errors = Validate(command);
            Assert.Empty(errors);
        }

        [Fact]
        public void RegisterUserCommand_SadPath_InvalidEmail_ReturnsValidationError()
        {
            var command = new RegisterUserCommand(
                "not-an-email", "SecurePass1!", "Juan",
                175, 80, 75, 28, 'M', ActivityLevel.ModeratelyActive);
            var errors = Validate(command);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void RegisterUserCommand_SadPath_PasswordTooShort_ReturnsValidationError()
        {
            var command = new RegisterUserCommand(
                "juan@example.com", "short", "Juan",
                175, 80, 75, 28, 'M', ActivityLevel.ModeratelyActive);
            var errors = Validate(command);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void RegisterUserCommand_SadPath_NameTooShort_ReturnsValidationError()
        {
            var command = new RegisterUserCommand(
                "juan@example.com", "SecurePass1!", "J",
                175, 80, 75, 28, 'M', ActivityLevel.ModeratelyActive);
            var errors = Validate(command);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void RegisterUserCommand_SadPath_HeightOutOfRange_ReturnsValidationError()
        {
            var command = new RegisterUserCommand(
                "juan@example.com", "SecurePass1!", "Juan",
                10, 80, 75, 28, 'M', ActivityLevel.ModeratelyActive); // height < 50
            var errors = Validate(command);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void RegisterUserCommand_SadPath_WeightOutOfRange_ReturnsValidationError()
        {
            var command = new RegisterUserCommand(
                "juan@example.com", "SecurePass1!", "Juan",
                175, 5, 75, 28, 'M', ActivityLevel.ModeratelyActive); // weight < 20
            var errors = Validate(command);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void RegisterUserCommand_SadPath_AgeOutOfRange_ReturnsValidationError()
        {
            var command = new RegisterUserCommand(
                "juan@example.com", "SecurePass1!", "Juan",
                175, 80, 75, 0, 'M', ActivityLevel.ModeratelyActive); // age < 1
            var errors = Validate(command);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void RegisterUserCommand_SadPath_InvalidBiologicalSex_ReturnsValidationError()
        {
            var command = new RegisterUserCommand(
                "juan@example.com", "SecurePass1!", "Juan",
                175, 80, 75, 28, 'X', ActivityLevel.ModeratelyActive); // invalid sex
            var errors = Validate(command);
            Assert.NotEmpty(errors);
        }
    }
}

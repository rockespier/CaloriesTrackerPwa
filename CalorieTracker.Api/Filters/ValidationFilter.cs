using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CalorieTracker.Api.Filters
{
    public class ValidationFilter<T> : IEndpointFilter where T : class
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var argument = context.Arguments.OfType<T>().FirstOrDefault();

            if (argument is null)
            {
                return Results.BadRequest(new { Message = "El cuerpo de la solicitud es requerido." });
            }

            var validationContext = new ValidationContext(argument);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true))
            {
                var errors = validationResults
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(r => r.ErrorMessage ?? string.Empty).ToArray()
                    );

                return Results.ValidationProblem(errors);
            }

            return await next(context);
        }
    }
}

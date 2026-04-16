using System.ComponentModel.DataAnnotations;
using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.Commands;

public record RegisterUserCommand(
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(255)]
    string Email,

    [property: Required]
    [property: StringLength(100, MinimumLength = 8)]
    string Password,

    [property: Required]
    [property: StringLength(100, MinimumLength = 2
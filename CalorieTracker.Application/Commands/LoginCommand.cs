using System.ComponentModel.DataAnnotations;

namespace CalorieTracker.Application.Commands;

public record LoginCommand(
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(255)]
    string Email,

    [property: Required]
    [property: StringLength(100, MinimumLength = 8)]
    string Password
);
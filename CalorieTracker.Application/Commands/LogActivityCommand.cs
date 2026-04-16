using System;

namespace CalorieTracker.Application.Commands
{
    public record LogActivityCommand(Guid UserId, string ActivityDescription, int DurationMinutes);
}

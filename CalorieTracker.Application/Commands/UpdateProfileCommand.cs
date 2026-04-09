using CalorieTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalorieTracker.Application.Commands
{
    public record UpdateProfileCommand
    (
    double Weight,
    double Height,
    int Age,
    char Gender,
    ActivityLevel ActivityLevel,
    UserGoal Goal
    );
}

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
    double weight,
    double height,
    int age,
    char gender,
    ActivityLevel activityLevel,
    string goal, // "Perder", "Mantener", "Ganar"
    int dailyCaloricTarget
    );
}

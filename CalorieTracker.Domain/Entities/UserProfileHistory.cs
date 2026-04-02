using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalorieTracker.Domain.Entities
{
    public class UserProfileHistory
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
        public ActivityLevel ActivityLevel { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }

}

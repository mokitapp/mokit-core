using Mokit.Domain.Common;
using Mokit.Domain.Enums;

namespace Mokit.Domain.Entities;

public class TeamMember : BaseEntity
{
    public Guid TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public TeamRole Role { get; set; } = TeamRole.Member;
    public bool IsActive { get; set; } = true;
    public DateTime? JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Team Team { get; set; } = null!;
}



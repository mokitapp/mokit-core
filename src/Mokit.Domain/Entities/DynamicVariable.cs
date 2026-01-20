using Mokit.Domain.Common;

namespace Mokit.Domain.Entities;

public class DynamicVariable : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Expression { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsActive { get; set; } = true;
    
    // For stateful variables
    public bool IsPersistent { get; set; } = false;
    public string? CurrentValue { get; set; }

    // Navigation properties
    public virtual MockProject Project { get; set; } = null!;
}



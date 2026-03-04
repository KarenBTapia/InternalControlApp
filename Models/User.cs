using System;
using System.Collections.Generic;

namespace InternalControlApp.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<ImprovementActionsPtci> ImprovementActionsPtcis { get; set; } = new List<ImprovementActionsPtci>();

    public virtual ICollection<RiskFactorsPtar> RiskFactorsPtars { get; set; } = new List<RiskFactorsPtar>();

    public virtual Role Role { get; set; } = null!;
}

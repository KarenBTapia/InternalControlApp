using System;
using System.Collections.Generic;

namespace InternalControlApp.Models;

public partial class AdministrativeUnit
{
    public int UnitId { get; set; }

    public string UnitName { get; set; } = null!;

    public virtual ICollection<ImprovementActionsPtci> ImprovementActionsPtcis { get; set; } = new List<ImprovementActionsPtci>();

    public virtual ICollection<RiskFactorsPtar> RiskFactorsPtars { get; set; } = new List<RiskFactorsPtar>();
}

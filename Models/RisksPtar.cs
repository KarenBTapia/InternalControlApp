using System;
using System.Collections.Generic;

namespace InternalControlApp.Models;

public partial class RisksPtar
{
    public int RiskId { get; set; }

    public string RiskNumber { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? RiskClassification { get; set; }

    public int? ImpactGrade { get; set; }

    public int? OccurrenceProbability { get; set; }

    public string? Quadrant { get; set; }

    public string? Strategy { get; set; }

    public virtual ICollection<RiskFactorsPtar> RiskFactorsPtars { get; set; } = new List<RiskFactorsPtar>();
}

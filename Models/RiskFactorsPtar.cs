using System;
using System.Collections.Generic;

namespace InternalControlApp.Models;

public partial class RiskFactorsPtar
{
    public int FactorId { get; set; }

    public int RiskId { get; set; }

    public string? FactorNumber { get; set; }

    public string? FactorDescription { get; set; }

    public string ControlAction { get; set; } = null!;

    public string? VerificationMeans { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal? ProgressPercentage { get; set; }

    public decimal? Quarter1Grade { get; set; }

    public decimal? Quarter2Grade { get; set; }

    public decimal? Quarter3Grade { get; set; }

    public decimal? Quarter4Grade { get; set; }

    public decimal? Quarter1GradeOic { get; set; }

    public decimal? Quarter2GradeOic { get; set; }

    public decimal? Quarter3GradeOic { get; set; }

    public decimal? Quarter4GradeOic { get; set; }

    public int ResponsibleUserId { get; set; }

    public int UnitId { get; set; }

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual User ResponsibleUser { get; set; } = null!;

    public virtual RisksPtar Risk { get; set; } = null!;

    public virtual AdministrativeUnit Unit { get; set; } = null!;
}

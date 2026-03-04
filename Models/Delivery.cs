using System;
using System.Collections.Generic;

namespace InternalControlApp.Models;

public partial class Delivery
{
    public int DeliveryId { get; set; }

    public int UserId { get; set; }

    public int? ActionIdPtci { get; set; }

    public int? FactorIdPtar { get; set; }

    public int? QuarterNumber { get; set; }

    public DateTime SubmissionDate { get; set; }

    public string Status { get; set; } = null!;

    public int? Grade { get; set; }

    public string? DirectorFeedback { get; set; }

    public string? UserComment { get; set; }

    // --- LÍNEA AÑADIDA ---
    public DateTime? ReviewDate { get; set; }

    public bool IsHiddenForEnlace { get; set; }
    public bool IsHiddenForCoordinator { get; set; }

    public virtual ImprovementActionsPtci? ActionIdPtciNavigation { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual RiskFactorsPtar? FactorIdPtarNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}

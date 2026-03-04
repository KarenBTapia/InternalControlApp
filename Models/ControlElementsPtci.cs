using System;
using System.Collections.Generic;

namespace InternalControlApp.Models;

public partial class ControlElementsPtci
{
    public int ElementId { get; set; }

    public string? Ngci { get; set; }

    public string? ControlNumber { get; set; }

    public string ControlElement { get; set; } = null!;

    public virtual ICollection<ImprovementActionsPtci> ImprovementActionsPtcis { get; set; } = new List<ImprovementActionsPtci>();
}

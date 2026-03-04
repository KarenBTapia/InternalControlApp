using System;
using System.Collections.Generic;

namespace InternalControlApp.Models;

public partial class Attachment
{
    public int AttachmentId { get; set; }

    public int DeliveryId { get; set; }

    public string OriginalFileName { get; set; } = null!;

    public string StoragePath { get; set; } = null!;

    public virtual Delivery Delivery { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternalControlApp.Models
{
    public class ReviewDeliveryViewModel
    {
        public int DeliveryId { get; set; }
        public string UserName { get; set; } = "";
        public DateTime SubmissionDate { get; set; }
        public string ProgramType { get; set; } = "";
        public string ParentTaskNumber { get; set; } = "";
        public string ParentTaskDescription { get; set; } = "";
        public string TaskDescription { get; set; } = "";
        public int QuarterNumber { get; set; }
        public string ContextualInfoLabel { get; set; } = "";
        public string? ContextualInfoText { get; set; }
        public string? UserComment { get; set; }
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();

        [Display(Name = "Retroalimentación")]
        public string? DirectorFeedback { get; set; }

        public bool IsReadOnly { get; set; }
    }
}
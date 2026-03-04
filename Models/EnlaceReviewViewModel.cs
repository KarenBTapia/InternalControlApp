namespace InternalControlApp.Models
{
    public class EnlaceReviewViewModel
    {
        public string ProgramType { get; set; } = "";
        public int ParentTaskId { get; set; } // ID del Elemento (PTCI) o Riesgo (PTAR)
        public string ParentTaskNumber { get; set; } = "";
        public string ParentTaskDescription { get; set; } = "";
        public string TaskDescription { get; set; } = "";
        public int QuarterNumber { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Status { get; set; } = "";
        public string? UserComment { get; set; }
        public string? DirectorFeedback { get; set; }
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
namespace SindhBankCareerForm.Models
{
    public class JobPostingViewModel
    {
        public string? Position { get; set; }
        public string? Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class JobPostingListItem
    {
        public int JobPostingId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrentlyOpen { get; set; }

        // True only when the End Date hasn't fully passed yet — meaning if
        // HR clicks "Reopen", it will actually have a visible effect
        // (posting goes back to Open). If the end date is already in the
        // past, reopening it wouldn't change anything visible, so we hide
        // the button in that case rather than show a "dead" toggle.
        public bool CanReopen => EndDate.Date >= DateTime.Today;
    }

    public class JobPostingsPageViewModel
    {
        public JobPostingViewModel NewPosting { get; set; } = new();
        public List<JobPostingListItem> Postings { get; set; } = new();

        // "open" or "closed" — which tab is currently selected on the page
        public string ViewMode { get; set; } = "open";
    }
}
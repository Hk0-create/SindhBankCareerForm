namespace SindhBankCareerForm.Models
{
    public class CvSearchViewModel
    {
        public string? PositionAppliedFor { get; set; }
        public string? JobLocation { get; set; }
        public string? ApplicationNumber { get; set; }
        public string? Name { get; set; }
        public string? CnicNo { get; set; }
        public string? MobileNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public bool HasSearched { get; set; }
        public List<CvSearchResultItem> Results { get; set; } = new();

        public List<string> AvailablePositions { get; set; } = new();
        public List<string> AvailableLocations { get; set; } = new();
    }

    public class CvSearchResultItem
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? PositionAppliedFor { get; set; }
        public string? JobLocation { get; set; }
        public string? CnicNo { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? RelativeInSindhBank { get; set; }
        public string? Domicile { get; set; }
        public string? CityOfResidence { get; set; }
        public string? PreviouslyEmployeeOfSindhBank { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string? Religion { get; set; }
        public string? SecondaryEducation { get; set; }
        public string? HigherSecondaryEducation { get; set; }
        public string? Bachelors { get; set; }
        public string? Masters { get; set; }
        public string? OtherQualification { get; set; }
        public string? BankingExperienceYears { get; set; }
        public string? CurrentOrganization { get; set; }
        public string? CurrentDesignation { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? Disability { get; set; }
        public string? DisabilityType { get; set; }
        public string? DisabilityDescription { get; set; }
        public bool HasCv { get; set; }
    }
}
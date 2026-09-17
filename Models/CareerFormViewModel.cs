namespace SindhBankCareerForm.Models
{
    public class CareerFormViewModel
    {
        public string? PositionAppliedFor { get; set; }
        public string? JobLocation { get; set; }

        public string? Name { get; set; }
        public string? CnicNo { get; set; }
        public string? DobYear { get; set; }
        public string? DobMonth { get; set; }
        public string? DobDay { get; set; }
        public string? Gender { get; set; }
        public string? CityOfResidence { get; set; }
        public string? Domicile { get; set; }
        public string? MobileNumber { get; set; }
        public string? ResidenceNumber { get; set; }
        public string? Email { get; set; }
        public IFormFile? CvFile { get; set; }
        public string? Address { get; set; }
        public string? Religion { get; set; }

        public string? SecondaryEducation { get; set; }
        public string? SecondaryInstituteBoard { get; set; }
        public string? HigherSecondaryEducation { get; set; }
        public string? HigherSecondaryInstituteBoard { get; set; }
        public string? Bachelors { get; set; }
        public string? BachelorsInstituteBoard { get; set; }
        public string? Masters { get; set; }
        public string? MastersInstituteBoard { get; set; }
        public string? OtherQualification { get; set; }
        public string? OtherQualificationInstituteBoard { get; set; }

        public string? TotalExperienceYears { get; set; }
        public string? CurrentOrganization { get; set; }
        public string? CurrentDesignation { get; set; }
        public string? BankingExperienceYears { get; set; }
        public string? RelativeInSindhBank { get; set; }
        public string? PreviouslyEmployeeOfSindhBank { get; set; }
        public string? Disability { get; set; }
        public string? DisabilityType { get; set; }
        public string? DisabilityDescription { get; set; }

        public bool IsSubmitted { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using SindhBankCareerForm.Models;

namespace SindhBankCareerForm.Controllers
{
    public class CareerController : Controller
    {
        private readonly string _connectionString;
        private readonly string _cvStorageFolder;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public CareerController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("SindhBankCareerForm")
                ?? throw new InvalidOperationException("Connection string 'SindhBankCareerForm' not found in appsettings.json.");

            _configuration = configuration;
            _environment = environment;

            _cvStorageFolder = Path.Combine(environment.ContentRootPath, "CvUploads");
            Directory.CreateDirectory(_cvStorageFolder);
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new CareerFormViewModel();
            if (TempData["ReferenceNumber"] is string referenceNumber)
            {
                model.IsSubmitted = true;
                model.ReferenceNumber = referenceNumber;
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveJobPostings()
        {
            var postings = new List<object>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetActiveJobPostings", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                postings.Add(new
                {
                    position = reader.GetString(reader.GetOrdinal("Position")),
                    location = reader.GetString(reader.GetOrdinal("Location"))
                });
            }
            return Json(postings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CareerFormViewModel model)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int? jobPostingId = await FindActiveJobPostingIdAsync(connection, model.PositionAppliedFor, model.JobLocation);
            if (jobPostingId is null)
            {
                ModelState.AddModelError(string.Empty,
                    "The selected position/location is no longer open for applications. Please choose a current option.");
                return View(model);
            }

            var missingFields = new List<string>();
            void RequireField(string? value, string label)
            {
                if (string.IsNullOrWhiteSpace(value)) missingFields.Add(label);
            }

            RequireField(model.Name, "Name");
            RequireField(model.CnicNo, "CNIC No");
            RequireField(model.Gender, "Gender");
            RequireField(model.CityOfResidence, "City of Residence");
            RequireField(model.Domicile, "Domicile");
            RequireField(model.MobileNumber, "Mobile Number");
            RequireField(model.ResidenceNumber, "Residence Number");
            RequireField(model.Email, "Email");
            RequireField(model.Address, "Address");
            RequireField(model.Religion, "Religion");
            RequireField(model.SecondaryEducation, "Secondary Education");
            RequireField(model.SecondaryInstituteBoard, "Secondary Institute/Board");
            RequireField(model.HigherSecondaryEducation, "Higher Secondary Education");
            RequireField(model.HigherSecondaryInstituteBoard, "Higher Secondary Institute/Board");
            RequireField(model.Bachelors, "Bachelors");
            RequireField(model.BachelorsInstituteBoard, "Bachelors Institute/Board");
            RequireField(model.OtherQualification, "Other Qualification");
            RequireField(model.OtherQualificationInstituteBoard, "Other Qualification Institute/Board");
            RequireField(model.TotalExperienceYears, "Total Experience");
            RequireField(model.CurrentOrganization, "Current Organization");
            RequireField(model.CurrentDesignation, "Current Designation");
            RequireField(model.BankingExperienceYears, "Banking Experience");
            RequireField(model.RelativeInSindhBank, "Relative In Sindh Bank");
            RequireField(model.PreviouslyEmployeeOfSindhBank, "Previously Employee of Sindh Bank");

            if (model.CvFile is null || model.CvFile.Length == 0)
            {
                missingFields.Add("CV Upload");
            }

            if (model.Disability == "Yes")
            {
                RequireField(model.DisabilityType, "Disability Type");
                RequireField(model.DisabilityDescription, "Disability Description");
            }

            if (missingFields.Count > 0)
            {
                ModelState.AddModelError(string.Empty, "Please fill in all required fields: " + string.Join(", ", missingFields));
                return View(model);
            }

            string? savedFileName = null;
            if (model.CvFile is { Length: > 0 })
            {
                var extension = Path.GetExtension(model.CvFile.FileName);
                savedFileName = $"{Guid.NewGuid()}{extension}";
                var fullPath = Path.Combine(_cvStorageFolder, savedFileName);
                await using var stream = new FileStream(fullPath, FileMode.Create);
                await model.CvFile.CopyToAsync(stream);
            }

            DateTime? dateOfBirth = null;
            if (int.TryParse(model.DobYear, out var y) &&
                int.TryParse(model.DobMonth, out var m) &&
                int.TryParse(model.DobDay, out var d))
            {
                try { dateOfBirth = new DateTime(y, m, d); }
                catch (ArgumentOutOfRangeException) { dateOfBirth = null; }
            }

            using var command = new SqlCommand("sp_InsertApplication", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@PositionAppliedFor", (object?)model.PositionAppliedFor ?? DBNull.Value);
            command.Parameters.AddWithValue("@JobLocation", (object?)model.JobLocation ?? DBNull.Value);
            command.Parameters.AddWithValue("@Name", (object?)model.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("@CnicNo", (object?)model.CnicNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateOfBirth", (object?)dateOfBirth ?? DBNull.Value);
            command.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
            command.Parameters.AddWithValue("@CityOfResidence", (object?)model.CityOfResidence ?? DBNull.Value);
            command.Parameters.AddWithValue("@Domicile", (object?)model.Domicile ?? DBNull.Value);
            command.Parameters.AddWithValue("@MobileNumber", (object?)model.MobileNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@ResidenceNumber", (object?)model.ResidenceNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@CvFilePath", (object?)savedFileName ?? DBNull.Value);
            command.Parameters.AddWithValue("@Address", (object?)model.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@Religion", (object?)model.Religion ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryEducation", (object?)model.SecondaryEducation ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryInstituteBoard", (object?)model.SecondaryInstituteBoard ?? DBNull.Value);
            command.Parameters.AddWithValue("@HigherSecondaryEducation", (object?)model.HigherSecondaryEducation ?? DBNull.Value);
            command.Parameters.AddWithValue("@HigherSecondaryInstituteBoard", (object?)model.HigherSecondaryInstituteBoard ?? DBNull.Value);
            command.Parameters.AddWithValue("@Bachelors", (object?)model.Bachelors ?? DBNull.Value);
            command.Parameters.AddWithValue("@BachelorsInstituteBoard", (object?)model.BachelorsInstituteBoard ?? DBNull.Value);
            command.Parameters.AddWithValue("@Masters", (object?)model.Masters ?? DBNull.Value);
            command.Parameters.AddWithValue("@MastersInstituteBoard", (object?)model.MastersInstituteBoard ?? DBNull.Value);
            command.Parameters.AddWithValue("@OtherQualification", (object?)model.OtherQualification ?? DBNull.Value);
            command.Parameters.AddWithValue("@OtherQualificationInstituteBoard", (object?)model.OtherQualificationInstituteBoard ?? DBNull.Value);
            command.Parameters.AddWithValue("@TotalExperienceYears", (object?)model.TotalExperienceYears ?? DBNull.Value);
            command.Parameters.AddWithValue("@CurrentOrganization", (object?)model.CurrentOrganization ?? DBNull.Value);
            command.Parameters.AddWithValue("@CurrentDesignation", (object?)model.CurrentDesignation ?? DBNull.Value);
            command.Parameters.AddWithValue("@BankingExperienceYears", (object?)model.BankingExperienceYears ?? DBNull.Value);
            command.Parameters.AddWithValue("@RelativeInSindhBank", (object?)model.RelativeInSindhBank ?? DBNull.Value);
            command.Parameters.AddWithValue("@PreviouslyEmployeeOfSindhBank", (object?)model.PreviouslyEmployeeOfSindhBank ?? DBNull.Value);
            command.Parameters.AddWithValue("@Disability", (object?)model.Disability ?? DBNull.Value);
            command.Parameters.AddWithValue("@DisabilityType", (object?)model.DisabilityType ?? DBNull.Value);
            command.Parameters.AddWithValue("@DisabilityDescription", (object?)model.DisabilityDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@JobPostingId", jobPostingId.Value);

            var applicationNumberParam = new SqlParameter("@ApplicationNumber", SqlDbType.NVarChar, 20)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(applicationNumberParam);

            await command.ExecuteNonQueryAsync();

            var referenceNumber = applicationNumberParam.Value?.ToString() ?? string.Empty;

            // Fire off the confirmation email. This never blocks or fails
            // the submission itself — the application is already safely
            // saved in the database at this point, regardless of whether
            // the email succeeds or fails.
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                await SendConfirmationEmailAsync(model.Email, model.Name ?? "Applicant", referenceNumber);
            }

            TempData["ReferenceNumber"] = referenceNumber;
            return RedirectToAction(nameof(Index));
        }

        private static async Task<int?> FindActiveJobPostingIdAsync(SqlConnection connection, string? position, string? location)
        {
            if (string.IsNullOrWhiteSpace(position) || string.IsNullOrWhiteSpace(location))
            {
                return null;
            }

            using var command = new SqlCommand("sp_FindActiveJobPostingId", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Position", position.Trim());
            command.Parameters.AddWithValue("@Location", location.Trim());

            var result = await command.ExecuteScalarAsync();
            return result is int id ? id : null;
        }

        // Reads the external HTML template file, swaps {{Name}} and
        // {{ReferenceNumber}} for the real values, and sends it via
        // SMTP. Wrapped in try-catch so an email problem (wrong
        // credentials, network hiccup) never breaks the applicant's
        // submission — their data is already safely in the database.
        private async Task SendConfirmationEmailAsync(string toEmail, string name, string referenceNumber)
        {
            try
            {
                var templatePath = Path.Combine(_environment.ContentRootPath, "EmailTemplates", "ApplicationConfirmation.html");
                var templateHtml = await System.IO.File.ReadAllTextAsync(templatePath);

                var emailBody = templateHtml
                    .Replace("{{Name}}", name)
                    .Replace("{{ReferenceNumber}}", referenceNumber);

                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["EmailSettings:FromName"] ?? "Sindh Bank Careers";

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? string.Empty, fromName),
                    Subject = "Sindh Bank - Application Received",
                    Body = emailBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send confirmation email: {ex.Message}");
            }
        }
    }
}
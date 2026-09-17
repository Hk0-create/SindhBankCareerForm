using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SindhBankCareerForm.Models;

namespace SindhBankCareerForm.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly string _connectionString;
        private readonly string _cvStorageFolder;

        public SearchController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("SindhBankCareerForm")
                ?? throw new InvalidOperationException("Connection string 'SindhBankCareerForm' not found in appsettings.json.");
            _cvStorageFolder = Path.Combine(environment.ContentRootPath, "CvUploads");
        }

        [HttpGet]
        public async Task<IActionResult> Index(CvSearchViewModel filters)
        {
            filters.AvailablePositions = await LoadDistinctAsync("sp_GetDistinctPositions", "PositionAppliedFor");
            filters.AvailableLocations = await LoadDistinctAsync("sp_GetDistinctLocations", "JobLocation");

            if (!filters.HasSearched)
            {
                return View(filters);
            }

            filters.Results = await RunSearchAsync(filters);
            return View(filters);
        }

        private async Task<List<string>> LoadDistinctAsync(string procedureName, string column)
        {
            var values = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(reader.GetOrdinal(column)));
            }

            return values;
        }

        private async Task<List<CvSearchResultItem>> RunSearchAsync(CvSearchViewModel filters)
        {
            var results = new List<CvSearchResultItem>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_SearchApplications", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@PositionAppliedFor", (object?)EmptyToNull(filters.PositionAppliedFor) ?? DBNull.Value);
            command.Parameters.AddWithValue("@JobLocation", (object?)EmptyToNull(filters.JobLocation) ?? DBNull.Value);
            command.Parameters.AddWithValue("@ApplicationNumber", (object?)EmptyToNull(filters.ApplicationNumber) ?? DBNull.Value);
            command.Parameters.AddWithValue("@Name", (object?)EmptyToNull(filters.Name) ?? DBNull.Value);
            command.Parameters.AddWithValue("@CnicNo", (object?)EmptyToNull(filters.CnicNo) ?? DBNull.Value);
            command.Parameters.AddWithValue("@MobileNumber", (object?)EmptyToNull(filters.MobileNumber) ?? DBNull.Value);
            command.Parameters.AddWithValue("@Gender", (object?)EmptyToNull(filters.Gender) ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateFrom", (object?)filters.DateFrom ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateTo", (object?)filters.DateTo ?? DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new CvSearchResultItem
                {
                    ApplicationId = reader.GetInt32(reader.GetOrdinal("ApplicationId")),
                    ApplicationNumber = reader.GetString(reader.GetOrdinal("ApplicationNumber")),
                    Name = ReadNullableString(reader, "Name"),
                    PositionAppliedFor = ReadNullableString(reader, "PositionAppliedFor"),
                    JobLocation = ReadNullableString(reader, "JobLocation"),
                    CnicNo = ReadNullableString(reader, "CnicNo"),
                    DateOfBirth = ReadNullableDate(reader, "DateOfBirth"),
                    Gender = ReadNullableString(reader, "Gender"),
                    RelativeInSindhBank = ReadNullableString(reader, "RelativeInSindhBank"),
                    Domicile = ReadNullableString(reader, "Domicile"),
                    CityOfResidence = ReadNullableString(reader, "CityOfResidence"),
                    PreviouslyEmployeeOfSindhBank = ReadNullableString(reader, "PreviouslyEmployeeOfSindhBank"),
                    SubmittedDate = reader.GetDateTime(reader.GetOrdinal("SubmittedDate")),
                    Religion = ReadNullableString(reader, "Religion"),
                    SecondaryEducation = ReadNullableString(reader, "SecondaryEducation"),
                    HigherSecondaryEducation = ReadNullableString(reader, "HigherSecondaryEducation"),
                    Bachelors = ReadNullableString(reader, "Bachelors"),
                    Masters = ReadNullableString(reader, "Masters"),
                    OtherQualification = ReadNullableString(reader, "OtherQualification"),
                    BankingExperienceYears = ReadNullableString(reader, "BankingExperienceYears"),
                    CurrentOrganization = ReadNullableString(reader, "CurrentOrganization"),
                    CurrentDesignation = ReadNullableString(reader, "CurrentDesignation"),
                    MobileNumber = ReadNullableString(reader, "MobileNumber"),
                    Email = ReadNullableString(reader, "Email"),
                    Disability = ReadNullableString(reader, "Disability"),
                    DisabilityType = ReadNullableString(reader, "DisabilityType"),
                    DisabilityDescription = ReadNullableString(reader, "DisabilityDescription"),
                    HasCv = reader.GetInt32(reader.GetOrdinal("HasCv")) == 1
                });
            }

            return results;
        }

        private static string? EmptyToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? ReadNullableString(SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static DateTime? ReadNullableDate(SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCv(int id)
        {
            string? cvFilePath = null;
            string? applicationNumber = null;
            string? name = null;

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(
                "SELECT CvFilePath, ApplicationNumber, Name FROM Applications WHERE ApplicationId = @Id", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    cvFilePath = ReadNullableString(reader, "CvFilePath");
                    applicationNumber = reader.GetString(reader.GetOrdinal("ApplicationNumber"));
                    name = ReadNullableString(reader, "Name");
                }
            }

            if (string.IsNullOrEmpty(cvFilePath))
            {
                return NotFound();
            }

            var fullPath = Path.Combine(_cvStorageFolder, cvFilePath);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var extension = Path.GetExtension(cvFilePath);
            var safeName = (name ?? "Applicant").Replace(" ", "_");
            var downloadName = $"{applicationNumber}-{safeName}{extension}";

            var contentType = extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(bytes, contentType, downloadName);
        }
    }
}
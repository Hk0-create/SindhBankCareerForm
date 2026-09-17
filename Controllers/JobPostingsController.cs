using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SindhBankCareerForm.Models;

namespace SindhBankCareerForm.Controllers
{
    [Authorize]
    public class JobPostingsController : Controller
    {
        private readonly string _connectionString;

        public JobPostingsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SindhBankCareerForm")
                ?? throw new InvalidOperationException("Connection string 'SindhBankCareerForm' not found in appsettings.json.");
        }

        [HttpGet]
        public async Task<IActionResult> Index(string view = "open")
        {
            // Load everything once, then split into two lists based on
            // IsCurrentlyOpen — a posting is "closed" if it was manually
            // deactivated by HR (IsActive = false) OR its date range has
            // simply expired (today is outside StartDate-EndDate). Either
            // way, IsCurrentlyOpen already accounts for both cases (it's
            // computed in sp_GetAllJobPostings), so we just filter on it.
            var allPostings = await LoadPostingsAsync();
            var viewMode = view == "closed" ? "closed" : "open";

            var model = new JobPostingsPageViewModel
            {
                Postings = viewMode == "open"
                    ? allPostings.Where(p => p.IsCurrentlyOpen).ToList()
                    : allPostings.Where(p => !p.IsCurrentlyOpen).ToList(),
                ViewMode = viewMode
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobPostingViewModel newPosting)
        {
            if (string.IsNullOrWhiteSpace(newPosting.Position) ||
                string.IsNullOrWhiteSpace(newPosting.Location) ||
                newPosting.StartDate is null || newPosting.EndDate is null)
            {
                TempData["JobPostingError"] = "Please fill in Position, Location, Start Date and End Date.";
                return RedirectToAction(nameof(Index));
            }

            if (newPosting.EndDate < newPosting.StartDate)
            {
                TempData["JobPostingError"] = "End date cannot be before the start date.";
                return RedirectToAction(nameof(Index));
            }

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CreateJobPosting", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Position", newPosting.Position.Trim());
            command.Parameters.AddWithValue("@Location", newPosting.Location.Trim());
            command.Parameters.Add("@StartDate", SqlDbType.Date).Value = newPosting.StartDate.Value.Date;
            command.Parameters.Add("@EndDate", SqlDbType.Date).Value = newPosting.EndDate.Value.Date;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int jobPostingId)
        {
            await SetActiveAsync(jobPostingId, true);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int jobPostingId)
        {
            await SetActiveAsync(jobPostingId, false);
            return RedirectToAction(nameof(Index));
        }

        private async Task SetActiveAsync(int jobPostingId, bool isActive)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_SetJobPostingActive", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@JobPostingId", jobPostingId);
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        private async Task<List<JobPostingListItem>> LoadPostingsAsync()
        {
            var list = new List<JobPostingListItem>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAllJobPostings", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new JobPostingListItem
                {
                    JobPostingId = reader.GetInt32(reader.GetOrdinal("JobPostingId")),
                    Position = reader.GetString(reader.GetOrdinal("Position")),
                    Location = reader.GetString(reader.GetOrdinal("Location")),
                    StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                    EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    IsCurrentlyOpen = reader.GetInt32(reader.GetOrdinal("IsCurrentlyOpen")) == 1
                });
            }

            return list;
        }
    }
}
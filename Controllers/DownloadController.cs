using System.Data;
using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace SindhBankCareerForm.Controllers
{
    [Authorize]
    public class DownloadController : Controller
    {
        private readonly string _connectionString;
        private readonly string _cvStorageFolder;

        public DownloadController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("SindhBankCareerForm")
                ?? throw new InvalidOperationException("Connection string 'SindhBankCareerForm' not found in appsettings.json.");
            _cvStorageFolder = Path.Combine(environment.ContentRootPath, "CvUploads");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Same dynamic list used on the Search page — pulled from real
            // application data, so it always matches what applicants actually
            // submitted (instead of a hardcoded list that can drift out of sync).
            ViewData["AvailablePositions"] = await LoadDistinctPositionsAsync();
            return View();
        }

        private async Task<List<string>> LoadDistinctPositionsAsync()
        {
            var positions = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetDistinctPositions", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                positions.Add(reader.GetString(reader.GetOrdinal("PositionAppliedFor")));
            }

            return positions;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ByRange(string fromNumber, string toNumber)
        {
            if (string.IsNullOrWhiteSpace(fromNumber) || string.IsNullOrWhiteSpace(toNumber))
            {
                TempData["DownloadError"] = "Please provide both a from and to Application Number.";
                return RedirectToAction(nameof(Index));
            }

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetApplicationsByNumberRange", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@FromNumber", fromNumber.Trim());
            command.Parameters.AddWithValue("@ToNumber", toNumber.Trim());

            var items = await ReadCvItemsAsync(command);
            return await BuildZipResultAsync(items, $"CVs_{fromNumber}_to_{toNumber}.zip");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ByList(string numbersCsv)
        {
            if (string.IsNullOrWhiteSpace(numbersCsv))
            {
                TempData["DownloadError"] = "Please enter at least one Application Number.";
                return RedirectToAction(nameof(Index));
            }

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetApplicationsByNumberList", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@NumbersCsv", numbersCsv.Trim());

            var items = await ReadCvItemsAsync(command);
            return await BuildZipResultAsync(items, "CVs_SelectedApplications.zip");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ByPositionAndDate(string positionAppliedFor, DateTime? dateFrom, DateTime? dateTo)
        {
            if (string.IsNullOrWhiteSpace(positionAppliedFor))
            {
                TempData["DownloadError"] = "Please select a position.";
                return RedirectToAction(nameof(Index));
            }

            if (dateFrom is null || dateTo is null)
            {
                TempData["DownloadError"] = "Please provide both a from and to date.";
                return RedirectToAction(nameof(Index));
            }

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetApplicationsByPositionAndDateRange", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@PositionAppliedFor", positionAppliedFor.Trim());
            command.Parameters.Add("@DateFrom", SqlDbType.Date).Value = dateFrom.Value.Date;
            command.Parameters.Add("@DateTo", SqlDbType.Date).Value = dateTo.Value.Date;

            var items = await ReadCvItemsAsync(command);
            var safePosition = positionAppliedFor.Replace(" ", "_").Replace("/", "-");
            return await BuildZipResultAsync(items, $"CVs_{safePosition}_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.zip");
        }

        private async Task<List<(string ApplicationNumber, string? Name, string? CvFilePath)>> ReadCvItemsAsync(SqlCommand command)
        {
            var items = new List<(string, string?, string?)>();

            await command.Connection!.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var appNumber = reader.GetString(reader.GetOrdinal("ApplicationNumber"));
                var nameOrdinal = reader.GetOrdinal("Name");
                var cvOrdinal = reader.GetOrdinal("CvFilePath");
                var name = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal);
                var cvPath = reader.IsDBNull(cvOrdinal) ? null : reader.GetString(cvOrdinal);
                items.Add((appNumber, name, cvPath));
            }

            return items;
        }

        private async Task<IActionResult> BuildZipResultAsync(
            List<(string ApplicationNumber, string? Name, string? CvFilePath)> items, string zipFileName)
        {
            var withCv = items.Where(i => !string.IsNullOrEmpty(i.CvFilePath)).ToList();

            if (withCv.Count == 0)
            {
                TempData["DownloadError"] = items.Count == 0
                    ? "No applications matched your criteria."
                    : "Matching applications were found, but none of them have a CV on file.";
                return RedirectToAction(nameof(Index));
            }

            byte[] zipBytes;
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var item in withCv)
                    {
                        var fullPath = Path.Combine(_cvStorageFolder, item.CvFilePath!);
                        if (!System.IO.File.Exists(fullPath))
                        {
                            continue;
                        }

                        var extension = Path.GetExtension(item.CvFilePath);
                        var safeName = (item.Name ?? "Applicant").Replace(" ", "_");
                        var entryName = $"{item.ApplicationNumber}-{safeName}{extension}";

                        archive.CreateEntryFromFile(fullPath, entryName);
                    }
                }

                zipBytes = memoryStream.ToArray();
            }

            await Task.CompletedTask;
            return File(zipBytes, "application/zip", zipFileName);
        }
    }
}
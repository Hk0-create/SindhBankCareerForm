using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SindhBankCareerForm.Models;

namespace SindhBankCareerForm.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _connectionString = configuration.GetConnectionString("SindhBankCareerForm")
                ?? throw new InvalidOperationException("Connection string 'SindhBankCareerForm' not found in appsettings.json.");
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.RecaptchaSiteKey = _configuration["ReCaptcha:SiteKey"];
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // The view needs this again in case we return it below
            // (failed CAPTCHA or failed login) — the widget must
            // re-render with a valid site key either way.
            ViewBag.RecaptchaSiteKey = _configuration["ReCaptcha:SiteKey"];

            var recaptchaToken = Request.Form["g-recaptcha-response"].ToString();
            var isHuman = await VerifyRecaptchaAsync(recaptchaToken);
            if (!isHuman)
            {
                ModelState.AddModelError(string.Empty, "Please complete the CAPTCHA verification.");
                return View(model);
            }

            string? storedHash = null;
            string? fullName = null;

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(
                "SELECT PasswordHash, FullName FROM HrUsers WHERE Email = @Email", connection))
            {
                command.Parameters.AddWithValue("@Email", model.Email?.Trim() ?? string.Empty);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    storedHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                    fullName = reader.GetString(reader.GetOrdinal("FullName"));
                }
            }

            if (storedHash is null || model.Password is null ||
                !BCrypt.Net.BCrypt.Verify(model.Password, storedHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, fullName ?? model.Email!),
                new(ClaimTypes.Email, model.Email!)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // Calls Google's siteverify endpoint with our Secret Key (kept
        // server-side only, never sent to the browser) plus the token
        // the widget generated after the user completed the checkbox.
        // Google responds with { "success": true/false, ... }.
        private async Task<bool> VerifyRecaptchaAsync(string recaptchaToken)
        {
            if (string.IsNullOrWhiteSpace(recaptchaToken))
            {
                return false;
            }

            var secretKey = _configuration["ReCaptcha:SecretKey"];
            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsync(
                "https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = secretKey ?? string.Empty,
                    ["response"] = recaptchaToken
                }));

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            return document.RootElement.TryGetProperty("success", out var successProperty)
                && successProperty.GetBoolean();
        }
    }
}
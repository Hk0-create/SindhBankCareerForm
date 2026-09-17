# Sindh Bank — Career Form & HR Job Portal

ASP.NET Core MVC application with two parts:

1. **Career Form** (public) — anyone can apply for a currently-open position.
2. **HR Job Portal** (staff only, login required) — HR can manage job
   postings, search submitted CVs, and bulk-download CVs as zip files.

---

## Tech Stack

- **Framework**: ASP.NET Core MVC, .NET 10
- **Database**: SQL Server (raw ADO.NET + stored procedures — no ORM)
- **Auth**: Cookie authentication, BCrypt-hashed passwords
- **Spam protection**: Google reCAPTCHA v2 (checkbox) on HR login
- **Email**: SMTP (via `System.Net.Mail`), HTML template read from an
  external file so the email design/wording can be changed without
  touching or redeploying code
- **NuGet packages used**: `Microsoft.Data.SqlClient`, `BCrypt.Net-Next`

---

## Project Structure

```
Controllers/
  CareerController.cs      → Public career form (view, submit, email)
  AccountController.cs     → HR login/logout + reCAPTCHA verification
  DashboardController.cs   → HR landing page after login
  SearchController.cs      → Search CV's (filters + individual/selected download)
  DownloadController.cs    → Download CV's (bulk zip, 3 methods)
  JobPostingsController.cs → Manage Job Postings (create / open / close)

Models/
  CareerFormViewModel.cs
  LoginViewModel.cs
  CvSearchViewModel.cs
  JobPostingViewModel.cs

Views/
  Career/, Account/, Dashboard/, Search/, Download/, JobPostings/, Shared/

wwwroot/
  css/site.css      → Public career form styling
  css/portal.css    → HR portal styling
  images/           → Logo, career form banner, background

EmailTemplates/
  ApplicationConfirmation.html   → Confirmation email HTML (edit this
                                    file directly to change the email —
                                    no code change or redeploy needed)

CvUploads/          → Uploaded CV files land here at runtime
                       (created automatically, lives OUTSIDE wwwroot
                       so files are never reachable by direct URL —
                       only served through the authenticated
                       Download/Search controllers)

database-setup.sql  → Full database schema + stored procedures (run once)
```

---

## Setup Instructions

### 1. Prerequisites
- Visual Studio 2026 (or any IDE supporting .NET 10)
- SQL Server (Express/Developer/Full — any edition)
- SQL Server Management Studio (SSMS), recommended for running the setup script

### 2. Database
Open `database-setup.sql` in SSMS (connected to your target SQL Server
instance) and run it top to bottom. It creates the `SindhBankCareerForm`
database, all tables, all stored procedures, and one test HR login.

**⚠️ Before going live**, read the security note inside the script
(Section 5) about the seeded test account — it must be removed or
have its password changed before this is used in production.

### 3. Connection String
In `appsettings.json`, set:
```json
"ConnectionStrings": {
    "SindhBankCareerForm": "Server=YOUR_SERVER;Database=SindhBankCareerForm;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;"
}
```
Replace `YOUR_SERVER` with the actual SQL Server instance name (e.g.
`localhost`, `.\SQLEXPRESS`, or a named server). If the deployment
environment uses SQL Login instead of Windows Authentication, replace
`Trusted_Connection=True;` with `User Id=...;Password=...;`.

### 4. Secrets (reCAPTCHA + Email)
Two features need credentials that must **never** be committed to
source control or left in `appsettings.json`. They're configured via
**.NET User Secrets** instead:

In Visual Studio: right-click the project → **Manage User Secrets** →
paste and fill in:
```json
{
  "ReCaptcha": {
    "SiteKey": "your Google reCAPTCHA v2 Site Key",
    "SecretKey": "your Google reCAPTCHA v2 Secret Key"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "the sending account's email address",
    "SmtpPassword": "an App Password for that account (not its normal login password)",
    "FromEmail": "same as SmtpUsername (or a verified alias)",
    "FromName": "Sindh Bank Careers"
  }
}
```

**Getting a reCAPTCHA key pair**: go to
`google.com/recaptcha/admin`, register a new site, choose
**reCAPTCHA v2 → "I'm not a robot" Checkbox**, and add the domain(s)
this will run on (`localhost` for local testing; the real domain once
deployed).

**Getting an SMTP App Password (Gmail example)**: enable 2-Step
Verification on the sending Gmail account, then generate one at
`myaccount.google.com/apppasswords`. If Sindh Bank has its own mail
server, use those SMTP details instead — no code changes are needed,
only the values above.

### 5. Run
Open the solution, press F5 (or `dotnet run`). Two entry points work:
- `/` or `/sindhbankcareerform` → public career form
- `/account/login` → HR portal login

**Before testing the career form**, log in to the HR portal and add
at least one job posting via **Manage Job Postings** — the career
form's Position/Location dropdowns only show currently-open postings.

---

## Default Test HR Login
```
Email:    hr@sindhbank.com.pk
Password: HR@12345
```
Remove or change this before any live/production use (see the
security note in `database-setup.sql`).

---

## Feature Notes

- **Application Numbers** are generated as `SB-` + a 7-digit
  zero-padded sequence (e.g. `SB-0000001`), based on the database's
  internal identity column — gaps in the sequence (from deleted test
  rows) are normal and expected, identity values are never reused.
- **CV storage**: files are saved to disk in a `CvUploads` folder
  (outside `wwwroot`) with a randomly generated file name; only the
  file name is stored in the database. Files are only ever served
  through authenticated controller actions, never by direct URL.
- **Bulk CV downloads** are built as an in-memory zip (no temp files
  on disk). This is intentional and fine at current scale; if the
  number of CVs grows very large in the future (1000+), this should
  be switched to a file-based/streamed approach to avoid high memory
  use — flagged by the supervisor as a future improvement, not needed
  now.
- **Job Postings** control what appears on the public career form.
  A posting is "Open" only when it's marked active AND today's date
  falls within its Start/End date range; everything else shows as
  "Closed" on the HR side.
- **Search CV's** supports selecting specific results via checkboxes
  and bulk-downloading just those, reusing the same download logic
  as the "by list" bulk download method (no duplicate code).
- **CAPTCHA** only guards the HR login page — its purpose is to block
  automated/bot login attempts (brute-force, credential-stuffing), not
  to hide the login page itself (login URLs are inherently public).

---

## Known Limitations / Possible Future Work
- No password-reset flow for HR accounts yet (would need to be added
  before removing the seeded test account in a live environment).
- No UI for HR account creation — currently done via SQL insert only,
  intentional for an internal-only login system.
- Job posting dates can't currently be edited/extended after creation
  — a closed-by-expiry posting can't be "reopened" without a new
  posting or a future edit feature.
- Email sending failures are logged to the console but don't block
  form submission — the application is always saved successfully
  regardless of whether the confirmation email succeeds.

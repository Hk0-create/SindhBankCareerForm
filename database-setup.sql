-- ================================================================
-- Sindh Bank — Career Form + HR Job Portal
-- Complete database setup script
--
-- This is the FINAL, consolidated version of every table and
-- stored procedure used by the project. Run this top-to-bottom on
-- any SQL Server instance to recreate the database from scratch.
-- ================================================================


-- ================================================================
-- 1. DATABASE
-- ================================================================
CREATE DATABASE SindhBankCareerForm;
GO

USE SindhBankCareerForm;
GO


-- ================================================================
-- 2. TABLES
-- ================================================================

-- Every career form submission is one row here.
CREATE TABLE Applications
(
    ApplicationId       INT IDENTITY(1,1) PRIMARY KEY,
    ApplicationNumber   NVARCHAR(20)   NULL UNIQUE,   -- e.g. SB-0000001, generated after insert

    PositionAppliedFor  NVARCHAR(100),
    JobLocation         NVARCHAR(100),

    Name                NVARCHAR(150),
    CnicNo              NVARCHAR(20),
    DateOfBirth         DATE,
    Gender              NVARCHAR(10),
    CityOfResidence     NVARCHAR(100),
    Domicile            NVARCHAR(100),
    MobileNumber        NVARCHAR(20),
    ResidenceNumber     NVARCHAR(20),
    Email               NVARCHAR(150),
    CvFilePath          NVARCHAR(300),                -- file name only; actual file lives in /CvUploads on disk
    Address             NVARCHAR(300),
    Religion            NVARCHAR(50),

    SecondaryEducation                 NVARCHAR(100),
    SecondaryInstituteBoard            NVARCHAR(150),
    HigherSecondaryEducation           NVARCHAR(100),
    HigherSecondaryInstituteBoard      NVARCHAR(150),
    Bachelors                          NVARCHAR(100),
    BachelorsInstituteBoard            NVARCHAR(150),
    Masters                            NVARCHAR(100),  -- optional field, not mandatory on the form
    MastersInstituteBoard              NVARCHAR(150),  -- optional field, not mandatory on the form
    OtherQualification                 NVARCHAR(300),
    OtherQualificationInstituteBoard   NVARCHAR(300),

    TotalExperienceYears            NVARCHAR(10),
    CurrentOrganization              NVARCHAR(150),
    CurrentDesignation                NVARCHAR(150),
    BankingExperienceYears           NVARCHAR(10),
    RelativeInSindhBank               NVARCHAR(150),
    PreviouslyEmployeeOfSindhBank    NVARCHAR(10),

    Disability                        NVARCHAR(10),    -- "Yes" / "No"
    DisabilityType                    NVARCHAR(100),   -- only filled when Disability = Yes
    DisabilityDescription             NVARCHAR(300),   -- only filled when Disability = Yes

    SubmittedDate       DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
    JobPostingId         INT NULL                       -- links to the exact JobPostings row applied against
);
GO

-- HR staff login credentials. No public self-registration — accounts
-- are created manually via SQL (or a future admin screen), matching
-- standard practice for internal/staff-only systems.
CREATE TABLE HrUsers
(
    HrUserId       INT IDENTITY(1,1) PRIMARY KEY,
    Email          NVARCHAR(150)  NOT NULL UNIQUE,
    PasswordHash   NVARCHAR(200)  NOT NULL,             -- BCrypt hash, never plain text
    FullName       NVARCHAR(150)  NOT NULL,
    CreatedDate    DATETIME2      NOT NULL DEFAULT (SYSDATETIME())
);
GO

-- HR-controlled job postings. The public career form only shows
-- Position/Location combinations that exist here and are currently
-- open (IsActive = 1 AND today is between StartDate and EndDate).
CREATE TABLE JobPostings
(
    JobPostingId    INT IDENTITY(1,1) PRIMARY KEY,
    Position        NVARCHAR(100) NOT NULL,
    Location        NVARCHAR(100) NOT NULL,
    StartDate       DATE NOT NULL,
    EndDate         DATE NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

ALTER TABLE Applications ADD CONSTRAINT FK_Applications_JobPostings
    FOREIGN KEY (JobPostingId) REFERENCES JobPostings(JobPostingId);
GO


-- ================================================================
-- 3. STORED PROCEDURES — Career Form (public side)
-- ================================================================

-- Inserts a new application, then generates and stores its
-- Application Number in the same call using the newly-created
-- row's identity value (format: SB- + 7-digit zero-padded number,
-- e.g. SB-0000001). Called from CareerController on form submit.
CREATE PROCEDURE sp_InsertApplication
    @PositionAppliedFor NVARCHAR(100) = NULL,
    @JobLocation NVARCHAR(100) = NULL,
    @Name NVARCHAR(150) = NULL,
    @CnicNo NVARCHAR(20) = NULL,
    @DateOfBirth DATE = NULL,
    @Gender NVARCHAR(10) = NULL,
    @CityOfResidence NVARCHAR(100) = NULL,
    @Domicile NVARCHAR(100) = NULL,
    @MobileNumber NVARCHAR(20) = NULL,
    @ResidenceNumber NVARCHAR(20) = NULL,
    @Email NVARCHAR(150) = NULL,
    @CvFilePath NVARCHAR(300) = NULL,
    @Address NVARCHAR(300) = NULL,
    @Religion NVARCHAR(50) = NULL,
    @SecondaryEducation NVARCHAR(100) = NULL,
    @SecondaryInstituteBoard NVARCHAR(150) = NULL,
    @HigherSecondaryEducation NVARCHAR(100) = NULL,
    @HigherSecondaryInstituteBoard NVARCHAR(150) = NULL,
    @Bachelors NVARCHAR(100) = NULL,
    @BachelorsInstituteBoard NVARCHAR(150) = NULL,
    @Masters NVARCHAR(100) = NULL,
    @MastersInstituteBoard NVARCHAR(150) = NULL,
    @OtherQualification NVARCHAR(300) = NULL,
    @OtherQualificationInstituteBoard NVARCHAR(300) = NULL,
    @TotalExperienceYears NVARCHAR(10) = NULL,
    @CurrentOrganization NVARCHAR(150) = NULL,
    @CurrentDesignation NVARCHAR(150) = NULL,
    @BankingExperienceYears NVARCHAR(10) = NULL,
    @RelativeInSindhBank NVARCHAR(150) = NULL,
    @PreviouslyEmployeeOfSindhBank NVARCHAR(10) = NULL,
    @Disability NVARCHAR(10) = NULL,
    @DisabilityType NVARCHAR(100) = NULL,
    @DisabilityDescription NVARCHAR(300) = NULL,
    @JobPostingId INT = NULL,
    @ApplicationNumber NVARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Applications
    (
        PositionAppliedFor, JobLocation, Name, CnicNo, DateOfBirth, Gender,
        CityOfResidence, Domicile, MobileNumber, ResidenceNumber, Email,
        CvFilePath, Address, Religion,
        SecondaryEducation, SecondaryInstituteBoard,
        HigherSecondaryEducation, HigherSecondaryInstituteBoard,
        Bachelors, BachelorsInstituteBoard,
        Masters, MastersInstituteBoard,
        OtherQualification, OtherQualificationInstituteBoard,
        TotalExperienceYears, CurrentOrganization, CurrentDesignation,
        BankingExperienceYears, RelativeInSindhBank,
        PreviouslyEmployeeOfSindhBank, Disability, DisabilityType, DisabilityDescription,
        JobPostingId
    )
    VALUES
    (
        @PositionAppliedFor, @JobLocation, @Name, @CnicNo, @DateOfBirth, @Gender,
        @CityOfResidence, @Domicile, @MobileNumber, @ResidenceNumber, @Email,
        @CvFilePath, @Address, @Religion,
        @SecondaryEducation, @SecondaryInstituteBoard,
        @HigherSecondaryEducation, @HigherSecondaryInstituteBoard,
        @Bachelors, @BachelorsInstituteBoard,
        @Masters, @MastersInstituteBoard,
        @OtherQualification, @OtherQualificationInstituteBoard,
        @TotalExperienceYears, @CurrentOrganization, @CurrentDesignation,
        @BankingExperienceYears, @RelativeInSindhBank,
        @PreviouslyEmployeeOfSindhBank, @Disability, @DisabilityType, @DisabilityDescription,
        @JobPostingId
    );

    DECLARE @NewId INT = SCOPE_IDENTITY();
    SET @ApplicationNumber = 'SB-' + RIGHT('0000000' + CAST(@NewId AS NVARCHAR(10)), 7);
    UPDATE Applications SET ApplicationNumber = @ApplicationNumber WHERE ApplicationId = @NewId;
END
GO

-- Confirms a Position+Location pair is a real, currently-open
-- posting before the application is saved — the real security gate
-- (a browser dropdown can always be tampered with client-side).
CREATE PROCEDURE sp_FindActiveJobPostingId
    @Position NVARCHAR(100),
    @Location NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 JobPostingId
    FROM JobPostings
    WHERE Position = @Position
        AND Location = @Location
        AND IsActive = 1
        AND CAST(GETDATE() AS DATE) BETWEEN StartDate AND EndDate;
END
GO

-- Feeds the career form's Position/Location dropdowns (only shows
-- what's actually open right now).
CREATE PROCEDURE sp_GetActiveJobPostings
AS
BEGIN
    SET NOCOUNT ON;
    SELECT JobPostingId, Position, Location
    FROM JobPostings
    WHERE IsActive = 1
        AND CAST(GETDATE() AS DATE) BETWEEN StartDate AND EndDate
    ORDER BY Position, Location;
END
GO


-- ================================================================
-- 4. STORED PROCEDURES — HR Job Portal
-- ================================================================

-- Search CV's page filters. Every parameter is optional — the
-- (@Param IS NULL OR Column = @Param) pattern means an unfilled
-- filter is simply ignored, so one procedure handles every
-- combination of filters.
CREATE PROCEDURE sp_SearchApplications
    @PositionAppliedFor NVARCHAR(100) = NULL,
    @JobLocation NVARCHAR(100) = NULL,
    @ApplicationNumber NVARCHAR(20) = NULL,
    @Name NVARCHAR(150) = NULL,
    @CnicNo NVARCHAR(20) = NULL,
    @MobileNumber NVARCHAR(20) = NULL,
    @Gender NVARCHAR(10) = NULL,
    @DateFrom DATE = NULL,
    @DateTo DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ApplicationId, ApplicationNumber, Name, PositionAppliedFor, JobLocation,
        CnicNo, DateOfBirth, Gender, RelativeInSindhBank, Domicile, CityOfResidence,
        PreviouslyEmployeeOfSindhBank, SubmittedDate, Religion, SecondaryEducation,
        HigherSecondaryEducation, Bachelors, Masters, OtherQualification,
        BankingExperienceYears, CurrentOrganization, CurrentDesignation,
        MobileNumber, Email, Disability, DisabilityType, DisabilityDescription,
        CASE WHEN CvFilePath IS NOT NULL AND LEN(CvFilePath) > 0 THEN 1 ELSE 0 END AS HasCv
    FROM Applications
    WHERE
        (@PositionAppliedFor IS NULL OR PositionAppliedFor = @PositionAppliedFor)
        AND (@JobLocation IS NULL OR JobLocation = @JobLocation)
        AND (@ApplicationNumber IS NULL OR ApplicationNumber LIKE '%' + @ApplicationNumber + '%')
        AND (@Name IS NULL OR Name LIKE '%' + @Name + '%')
        AND (@CnicNo IS NULL OR CnicNo LIKE '%' + @CnicNo + '%')
        AND (@MobileNumber IS NULL OR MobileNumber LIKE '%' + @MobileNumber + '%')
        AND (@Gender IS NULL OR Gender = @Gender)
        AND (@DateFrom IS NULL OR CAST(SubmittedDate AS DATE) >= @DateFrom)
        AND (@DateTo IS NULL OR CAST(SubmittedDate AS DATE) <= @DateTo)
    ORDER BY SubmittedDate DESC;
END
GO

-- Feeds the dynamic Position dropdowns on Search CV's and Download
-- CV's — always reflects real submitted data, never a hardcoded list.
CREATE PROCEDURE sp_GetDistinctPositions
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT PositionAppliedFor
    FROM Applications
    WHERE PositionAppliedFor IS NOT NULL AND LEN(PositionAppliedFor) > 0
    ORDER BY PositionAppliedFor;
END
GO

-- Feeds the dynamic Location dropdown on Search CV's.
CREATE PROCEDURE sp_GetDistinctLocations
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT JobLocation
    FROM Applications
    WHERE JobLocation IS NOT NULL AND LEN(JobLocation) > 0
    ORDER BY JobLocation;
END
GO

-- Download CV's — Method 1: by Application Number range.
CREATE PROCEDURE sp_GetApplicationsByNumberRange
    @FromNumber NVARCHAR(20),
    @ToNumber NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ApplicationId, ApplicationNumber, Name, CvFilePath
    FROM Applications
    WHERE ApplicationNumber BETWEEN @FromNumber AND @ToNumber
    ORDER BY ApplicationNumber;
END
GO

-- Download CV's — Method 2: by comma-separated Application Numbers.
-- Also reused by the "Download Selected" checkbox feature on the
-- Search CV's page (same procedure, same action, no duplicate code).
CREATE PROCEDURE sp_GetApplicationsByNumberList
    @NumbersCsv NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.ApplicationId, a.ApplicationNumber, a.Name, a.CvFilePath
    FROM Applications a
    INNER JOIN STRING_SPLIT(@NumbersCsv, ',') s
        ON a.ApplicationNumber = LTRIM(RTRIM(s.value))
    ORDER BY a.ApplicationNumber;
END
GO

-- Download CV's — Method 3: by Position + submission date range.
CREATE PROCEDURE sp_GetApplicationsByPositionAndDateRange
    @PositionAppliedFor NVARCHAR(100),
    @DateFrom DATE,
    @DateTo DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ApplicationId, ApplicationNumber, Name, CvFilePath
    FROM Applications
    WHERE PositionAppliedFor = @PositionAppliedFor
        AND CAST(SubmittedDate AS DATE) BETWEEN @DateFrom AND @DateTo
    ORDER BY SubmittedDate;
END
GO

-- Manage Job Postings — create a new posting.
CREATE PROCEDURE sp_CreateJobPosting
    @Position NVARCHAR(100),
    @Location NVARCHAR(100),
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO JobPostings (Position, Location, StartDate, EndDate, IsActive)
    VALUES (@Position, @Location, @StartDate, @EndDate, 1);
END
GO

-- Manage Job Postings — full list, with a computed IsCurrentlyOpen
-- flag (IsActive = 1 AND today is within the date range). The
-- Open/Closed tabs on the page split this list client-side.
CREATE PROCEDURE sp_GetAllJobPostings
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        JobPostingId, Position, Location, StartDate, EndDate, IsActive,
        CASE WHEN IsActive = 1 AND CAST(GETDATE() AS DATE) BETWEEN StartDate AND EndDate
             THEN 1 ELSE 0 END AS IsCurrentlyOpen
    FROM JobPostings
    ORDER BY CreatedDate DESC;
END
GO

-- Manage Job Postings — the Close / Reopen action. Called by two
-- separate controller actions (Activate/Deactivate) with @IsActive
-- fixed to true/false respectively, rather than trusting a single
-- boolean form field.
CREATE PROCEDURE sp_SetJobPostingActive
    @JobPostingId INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE JobPostings SET IsActive = @IsActive WHERE JobPostingId = @JobPostingId;
END
GO


-- ================================================================
-- 5. OPTIONAL — seed one test HR login so the portal can be tried
--    immediately after setup.
--
--    !! IMPORTANT — SECURITY NOTE BEFORE GOING LIVE !!
--    This is a DEMO account with a publicly-known password
--    (used throughout development/testing). Before deploying this
--    to a live/production environment:
--      - Either delete this row and insert real HR accounts with
--        freshly-generated BCrypt hashes, or
--      - At minimum, log in once and change this account's password
--        (requires adding a "change password" feature, or manually
--        updating PasswordHash with a new BCrypt hash).
--    Do NOT leave this demo account active on a live server.
-- ================================================================

-- Login: hr@sindhbank.com.pk   /   Password: HR@12345
INSERT INTO HrUsers (Email, PasswordHash, FullName) VALUES
    ('hr@sindhbank.com.pk', '$2b$12$4V/V1Y0A72H79qhKqzEUYelVz74XWUgIyvE547Ot4FNyAB2i5Bk3e', 'HR Test Account');
GO


-- ================================================================
-- Setup complete. Next steps (see README.md for full detail):
--   1. Update the connection string in appsettings.json / User
--      Secrets to point at this database.
--   2. Add ReCaptcha + EmailSettings values to User Secrets.
--   3. Add job postings via the HR Portal (Manage Job Postings)
--      before testing the public career form — it only shows
--      positions that exist there.
-- ================================================================

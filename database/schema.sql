IF OBJECT_ID('dbo.Quotes', 'U') IS NOT NULL
    DROP TABLE dbo.Quotes;

IF OBJECT_ID('dbo.Properties', 'U') IS NOT NULL
    DROP TABLE dbo.Properties;

IF OBJECT_ID('dbo.Clients', 'U') IS NOT NULL
    DROP TABLE dbo.Clients;
GO

CREATE TABLE dbo.Clients
(
    ClientID INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(254) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Clients_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Clients PRIMARY KEY CLUSTERED (ClientID),
    CONSTRAINT CK_Clients_FirstName_NotBlank CHECK (LEN(LTRIM(RTRIM(FirstName))) > 0),
    CONSTRAINT CK_Clients_LastName_NotBlank CHECK (LEN(LTRIM(RTRIM(LastName))) > 0),
    CONSTRAINT CK_Clients_Email_NotBlank CHECK (LEN(LTRIM(RTRIM(Email))) > 0)
);
GO

CREATE TABLE dbo.Properties
(
    PropertyID INT IDENTITY(1,1) NOT NULL,
    ClientID INT NOT NULL,
    Postcode VARCHAR(16) NOT NULL,
    Region VARCHAR(100) NULL,
    YearBuilt INT NOT NULL,
    RebuildCost DECIMAL(10,2) NOT NULL,
    IsUnoccupied BIT NOT NULL,

    CONSTRAINT PK_Properties PRIMARY KEY CLUSTERED (PropertyID),
    CONSTRAINT FK_Properties_Clients FOREIGN KEY (ClientID) REFERENCES dbo.Clients (ClientID),
    CONSTRAINT CK_Properties_Postcode_NotBlank CHECK (LEN(LTRIM(RTRIM(Postcode))) > 0),
    CONSTRAINT CK_Properties_YearBuilt_Range CHECK (YearBuilt BETWEEN 1500 AND 2100),
    CONSTRAINT CK_Properties_RebuildCost_Positive CHECK (RebuildCost > 0)
);
GO

CREATE TABLE dbo.Quotes
(
    QuoteID INT IDENTITY(1,1) NOT NULL,
    PropertyID INT NOT NULL,
    UnderwriterName VARCHAR(100) NOT NULL,
    PremiumAmount DECIMAL(10,2) NOT NULL,
    RiskRating VARCHAR(20) NOT NULL,
    Region VARCHAR(100) NULL,
    GeneratedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Quotes_GeneratedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Quotes PRIMARY KEY CLUSTERED (QuoteID),
    CONSTRAINT FK_Quotes_Properties FOREIGN KEY (PropertyID) REFERENCES dbo.Properties (PropertyID),
    CONSTRAINT CK_Quotes_UnderwriterName_NotBlank CHECK (LEN(LTRIM(RTRIM(UnderwriterName))) > 0),
    CONSTRAINT CK_Quotes_PremiumAmount_Positive CHECK (PremiumAmount > 0),
    CONSTRAINT CK_Quotes_RiskRating_Allowed CHECK (RiskRating IN ('Low', 'Medium', 'High'))
);
GO

CREATE INDEX IX_Clients_Email ON dbo.Clients (Email);
CREATE INDEX IX_Properties_ClientID ON dbo.Properties (ClientID);
CREATE INDEX IX_Quotes_PropertyID ON dbo.Quotes (PropertyID);
GO

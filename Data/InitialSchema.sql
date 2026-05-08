-- ═══════════════════════════════════════════════════════════════════════
-- BloodBankAccounting — Initial Database Setup
-- Run this ONLY if you're not using EF Migrations
-- Preferred: use `dotnet ef migrations add InitialCreate` then `Update-Database`
-- ═══════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'BloodBankAccounting')
    CREATE DATABASE BloodBankAccounting;
GO

USE BloodBankAccounting;
GO

-- Customers
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Customers')
CREATE TABLE Customers (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(200) NOT NULL,
    Type         NVARCHAR(50)  NOT NULL DEFAULT 'Hospital',
    Phone        NVARCHAR(50)  NOT NULL DEFAULT '',
    Address      NVARCHAR(500) NULL,
    Balance      DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreditLimit  DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
);

-- Safes
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Safes')
CREATE TABLE Safes (
    Id      INT IDENTITY(1,1) PRIMARY KEY,
    Name    NVARCHAR(100) NOT NULL,
    Type    NVARCHAR(50)  NOT NULL DEFAULT 'Cash',
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsMain  BIT           NOT NULL DEFAULT 0
);

INSERT INTO Safes (Name, Type, Balance, IsMain)
SELECT 'الخزينة الرئيسية', 'Cash', 0, 1
WHERE NOT EXISTS (SELECT 1 FROM Safes WHERE IsMain = 1);

-- BloodBags
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BloodBags')
CREATE TABLE BloodBags (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    BagNumber  NVARCHAR(50)  NOT NULL UNIQUE,
    BloodGroup NVARCHAR(50)  NOT NULL,
    CostPrice  DECIMAL(18,2) NOT NULL DEFAULT 0,
    SalePrice  DECIMAL(18,2) NOT NULL DEFAULT 0,
    ExpiryDate DATETIME2     NOT NULL,
    EntryDate  DATETIME2     NOT NULL DEFAULT GETDATE(),
    Status     NVARCHAR(50)  NOT NULL DEFAULT 'Available',
    Notes      NVARCHAR(MAX) NULL
);

-- Invoices
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Invoices')
CREATE TABLE Invoices (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber  NVARCHAR(20)  NOT NULL UNIQUE,
    Date           DATETIME2     NOT NULL DEFAULT GETDATE(),
    PaymentType    NVARCHAR(20)  NOT NULL DEFAULT 'Cash',
    CustomerId     INT           NOT NULL REFERENCES Customers(Id),
    SafeId         INT           NULL     REFERENCES Safes(Id),
    SubTotal       DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount    DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsCancelled    BIT           NOT NULL DEFAULT 0,
    CancelReason   NVARCHAR(500) NULL,
    Notes          NVARCHAR(MAX) NULL,
    CreatedAt      DATETIME2     NOT NULL DEFAULT GETDATE()
);

-- InvoiceLines
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='InvoiceLines')
CREATE TABLE InvoiceLines (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId  INT           NOT NULL REFERENCES Invoices(Id) ON DELETE CASCADE,
    BloodBagId INT           NULL REFERENCES BloodBags(Id),
    UnitPrice  DECIMAL(18,2) NOT NULL,
    Quantity   INT           NOT NULL DEFAULT 1,
    Total      DECIMAL(18,2) NOT NULL
);

-- ReceiptVouchers
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ReceiptVouchers')
CREATE TABLE ReceiptVouchers (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    VoucherNumber    NVARCHAR(20)  NOT NULL UNIQUE,
    Type             NVARCHAR(20)  NOT NULL DEFAULT 'Receipt',
    Date             DATETIME2     NOT NULL DEFAULT GETDATE(),
    SafeId           INT           NOT NULL REFERENCES Safes(Id),
    CustomerId       INT           NULL     REFERENCES Customers(Id),
    RelatedInvoiceId INT           NULL     REFERENCES Invoices(Id),
    Amount           DECIMAL(18,2) NOT NULL,
    Notes            NVARCHAR(MAX) NULL,
    CreatedAt        DATETIME2     NOT NULL DEFAULT GETDATE()
);

-- JournalEntries
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='JournalEntries')
CREATE TABLE JournalEntries (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    Date             DATETIME2     NOT NULL,
    AccountCode      NVARCHAR(20)  NOT NULL,
    AccountName      NVARCHAR(100) NOT NULL,
    DebitAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreditAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    RefType          NVARCHAR(50)  NOT NULL DEFAULT '',
    RefId            INT           NOT NULL DEFAULT 0,
    InvoiceId        INT           NULL REFERENCES Invoices(Id),
    ReceiptVoucherId INT           NULL REFERENCES ReceiptVouchers(Id),
    Notes            NVARCHAR(MAX) NULL,
    CreatedAt        DATETIME2     NOT NULL DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_JournalEntries_Ref' 
               AND object_id = OBJECT_ID('JournalEntries'))
    CREATE INDEX IX_JournalEntries_Ref ON JournalEntries(RefType, RefId);
PRINT '✓ Database setup complete';

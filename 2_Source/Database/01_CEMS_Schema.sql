-- ==============================================================================
-- Classroom Equipment Management System (CEMS)
-- Database Creation Script
-- Generated based on Physical_ERD_Design.md v1.0
-- DBMS: Microsoft SQL Server 2019+
-- Collation: Vietnamese_CI_AS
-- ==============================================================================

USE PRN_Project;
GO

-- ==============================================================================
-- 1. TABLES
-- ==============================================================================

-- 1. Users
CREATE TABLE Users (
    UserID          INT             NOT NULL IDENTITY(1,1),
    UserCode        NVARCHAR(20)    NOT NULL,
    FullName        NVARCHAR(150)   NOT NULL,
    Email           NVARCHAR(256)   NOT NULL,
    PasswordHash    NVARCHAR(256)   NOT NULL,
    PhoneNumber     NVARCHAR(20)    NULL,
    AvatarUrl       NVARCHAR(500)   NULL,
    Role            NVARCHAR(20)    NOT NULL
                        CONSTRAINT CHK_Users_Role
                        CHECK (Role IN ('Admin', 'Technician', 'Lecturer')),
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME2(0)    NULL,
    LastLoginAt     DATETIME2(0)    NULL,
    FailedLoginCount INT            NOT NULL DEFAULT 0,
    LockoutUntil    DATETIME2(0)    NULL,

    CONSTRAINT PK_Users PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_UserCode UNIQUE (UserCode),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email)
);

-- 2. Rooms
CREATE TABLE Rooms (
    RoomID      INT             NOT NULL IDENTITY(1,1),
    RoomCode    NVARCHAR(20)    NOT NULL,
    RoomName    NVARCHAR(150)   NOT NULL,
    Location    NVARCHAR(200)   NULL,
    Capacity    INT             NULL
                    CONSTRAINT CHK_Rooms_Capacity CHECK (Capacity > 0),
    RoomType    NVARCHAR(50)    NULL
                    CONSTRAINT CHK_Rooms_RoomType
                    CHECK (RoomType IN (N'Lý thuyết', N'Thực hành', N'Hội trường', N'Khác')),
    IsActive    BIT             NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2(0)    NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_Rooms PRIMARY KEY (RoomID),
    CONSTRAINT UQ_Rooms_RoomCode UNIQUE (RoomCode)
);

-- 3. EquipmentCategories
CREATE TABLE EquipmentCategories (
    CategoryID      INT             NOT NULL IDENTITY(1,1),
    CategoryName    NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(MAX)   NULL,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_EquipmentCategories PRIMARY KEY (CategoryID),
    CONSTRAINT UQ_EquipmentCategories_Name UNIQUE (CategoryName)
);

-- 4. Equipments
CREATE TABLE Equipments (
    EquipmentID     INT             NOT NULL IDENTITY(1,1),
    AssetCode       NVARCHAR(50)    NOT NULL,
    EquipmentName   NVARCHAR(150)   NOT NULL,
    CategoryID      INT             NOT NULL,
    CurrentRoomID   INT             NULL,
    SerialNumber    NVARCHAR(100)   NULL,
    Manufacturer    NVARCHAR(150)   NULL,
    Supplier        NVARCHAR(150)   NULL,
    PurchaseDate    DATE            NULL,
    WarrantyExpiry  DATE            NULL,
    Status          NVARCHAR(30)    NOT NULL DEFAULT 'InUse'
                        CONSTRAINT CHK_Equipments_Status
                        CHECK (Status IN (
                            'InUse', 'PendingRepair',
                            'UnderMaintenance', 'ProposedDisposal', 'Disposed'
                        )),
    IsActive        BIT             NOT NULL DEFAULT 1,
    Notes           NVARCHAR(MAX)   NULL,
    CreatedBy       INT             NOT NULL,
    UpdatedBy       INT             NULL,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME2(0)    NULL,

    CONSTRAINT PK_Equipments            PRIMARY KEY (EquipmentID),
    CONSTRAINT UQ_Equipments_AssetCode  UNIQUE (AssetCode),

    CONSTRAINT FK_Equipments_Category
        FOREIGN KEY (CategoryID) REFERENCES EquipmentCategories(CategoryID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Equipments_Room
        FOREIGN KEY (CurrentRoomID) REFERENCES Rooms(RoomID)
        ON DELETE SET NULL ON UPDATE NO ACTION,

    CONSTRAINT FK_Equipments_CreatedBy
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Equipments_UpdatedBy
        FOREIGN KEY (UpdatedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);

-- 5. TransferHistories
CREATE TABLE TransferHistories (
    TransferID      INT             NOT NULL IDENTITY(1,1),
    EquipmentID     INT             NOT NULL,
    FromRoomID      INT             NULL,
    ToRoomID        INT             NOT NULL,
    TransferredBy   INT             NOT NULL,
    TransferDate    DATETIME2(0)    NOT NULL DEFAULT GETDATE(),
    Reason          NVARCHAR(500)   NULL,

    CONSTRAINT PK_TransferHistories PRIMARY KEY (TransferID),

    CONSTRAINT FK_Transfer_Equipment
        FOREIGN KEY (EquipmentID) REFERENCES Equipments(EquipmentID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Transfer_FromRoom
        FOREIGN KEY (FromRoomID) REFERENCES Rooms(RoomID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Transfer_ToRoom
        FOREIGN KEY (ToRoomID) REFERENCES Rooms(RoomID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Transfer_TransferredBy
        FOREIGN KEY (TransferredBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT CHK_Transfer_Rooms
        CHECK (FromRoomID IS NULL OR FromRoomID != ToRoomID)
);

-- 6. EquipmentStatusLogs
CREATE TABLE EquipmentStatusLogs (
    LogID           INT             NOT NULL IDENTITY(1,1),
    EquipmentID     INT             NOT NULL,
    ChangedBy       INT             NOT NULL,
    OldStatus       NVARCHAR(30)    NULL,
    NewStatus       NVARCHAR(30)    NOT NULL,
    FieldChanged    NVARCHAR(100)   NULL,
    OldValue        NVARCHAR(500)   NULL,
    NewValue        NVARCHAR(500)   NULL,
    ChangeReason    NVARCHAR(500)   NULL,
    ChangedAt       DATETIME2(0)    NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_EquipmentStatusLogs PRIMARY KEY (LogID),

    CONSTRAINT FK_StatusLog_Equipment
        FOREIGN KEY (EquipmentID) REFERENCES Equipments(EquipmentID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_StatusLog_ChangedBy
        FOREIGN KEY (ChangedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);

-- 7. IncidentReports
CREATE TABLE IncidentReports (
    IncidentID      INT             NOT NULL IDENTITY(1,1),
    EquipmentID     INT             NOT NULL,
    RoomID          INT             NOT NULL,
    ReportedBy      INT             NOT NULL,
    AssignedTo      INT             NULL,
    Description     NVARCHAR(MAX)   NOT NULL,
    ImageUrl        NVARCHAR(500)   NULL,
    Status          NVARCHAR(30)    NOT NULL DEFAULT 'Pending'
                        CONSTRAINT CHK_Incident_Status
                        CHECK (Status IN ('Pending', 'InProgress', 'Resolved', 'Cancelled')),
    ReportedAt      DATETIME2(0)    NOT NULL DEFAULT GETDATE(),
    ResolvedAt      DATETIME2(0)    NULL,
    ResolutionNote  NVARCHAR(MAX)   NULL,
    DueDate         DATETIME2(0)    NULL,
    IsOverdue       BIT             NOT NULL DEFAULT 0,

    CONSTRAINT PK_IncidentReports PRIMARY KEY (IncidentID),

    CONSTRAINT FK_Incident_Equipment
        FOREIGN KEY (EquipmentID) REFERENCES Equipments(EquipmentID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Incident_Room
        FOREIGN KEY (RoomID) REFERENCES Rooms(RoomID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Incident_ReportedBy
        FOREIGN KEY (ReportedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Incident_AssignedTo
        FOREIGN KEY (AssignedTo) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT CHK_Incident_ResolvedAt
        CHECK (ResolvedAt IS NULL OR ResolvedAt >= ReportedAt)
);

-- 8. MaintenanceTickets
CREATE TABLE MaintenanceTickets (
    TicketID            INT             NOT NULL IDENTITY(1,1),
    IncidentID          INT             NOT NULL,
    EquipmentID         INT             NOT NULL,
    CreatedBy           INT             NOT NULL,
    ServiceProvider     NVARCHAR(200)   NOT NULL,
    EstimatedCost       DECIMAL(15, 2)  NULL
                            CONSTRAINT CHK_Ticket_EstCost CHECK (EstimatedCost >= 0),
    ActualCost          DECIMAL(15, 2)  NULL
                            CONSTRAINT CHK_Ticket_ActCost CHECK (ActualCost >= 0),
    SentDate            DATE            NOT NULL,
    ExpectedReturnDate  DATE            NOT NULL,
    ActualReturnDate    DATE            NULL,
    Status              NVARCHAR(30)    NOT NULL DEFAULT 'Sent'
                            CONSTRAINT CHK_Ticket_Status
                            CHECK (Status IN ('Sent', 'Returned', 'Cancelled')),
    Notes               NVARCHAR(MAX)   NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_MaintenanceTickets PRIMARY KEY (TicketID),
    CONSTRAINT UQ_Ticket_Incident    UNIQUE (IncidentID),

    CONSTRAINT FK_Ticket_Incident
        FOREIGN KEY (IncidentID) REFERENCES IncidentReports(IncidentID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Ticket_Equipment
        FOREIGN KEY (EquipmentID) REFERENCES Equipments(EquipmentID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Ticket_CreatedBy
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT CHK_Ticket_Dates
        CHECK (ExpectedReturnDate >= SentDate),

    CONSTRAINT CHK_Ticket_ActualReturn
        CHECK (ActualReturnDate IS NULL OR ActualReturnDate >= SentDate)
);

-- 9. DisposalRequests
CREATE TABLE DisposalRequests (
    DisposalID      INT             NOT NULL IDENTITY(1,1),
    EquipmentID     INT             NOT NULL,
    ProposedBy      INT             NOT NULL,
    ApprovedBy      INT             NULL,
    Reason          NVARCHAR(MAX)   NOT NULL,
    Status          NVARCHAR(30)    NOT NULL DEFAULT 'Pending'
                        CONSTRAINT CHK_Disposal_Status
                        CHECK (Status IN ('Pending', 'Approved', 'Rejected')),
    ProposedAt      DATETIME2(0)    NOT NULL DEFAULT GETDATE(),
    DecidedAt       DATETIME2(0)    NULL,
    AdminNote       NVARCHAR(500)   NULL,

    CONSTRAINT PK_DisposalRequests PRIMARY KEY (DisposalID),

    CONSTRAINT FK_Disposal_Equipment
        FOREIGN KEY (EquipmentID) REFERENCES Equipments(EquipmentID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Disposal_ProposedBy
        FOREIGN KEY (ProposedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Disposal_ApprovedBy
        FOREIGN KEY (ApprovedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT CHK_Disposal_DecidedAt
        CHECK (DecidedAt IS NULL OR DecidedAt >= ProposedAt)
);

-- 10. Notifications
CREATE TABLE Notifications (
    NotificationID      INT             NOT NULL IDENTITY(1,1),
    RecipientID         INT             NOT NULL,
    Title               NVARCHAR(200)   NOT NULL,
    Message             NVARCHAR(MAX)   NOT NULL,
    Type                NVARCHAR(50)    NOT NULL
                            CONSTRAINT CHK_Notif_Type
                            CHECK (Type IN (
                                'IncidentReported', 'IncidentResolved',
                                'PasswordReset', 'WarrantyExpiry',
                                'DisposalProposed', 'DisposalDecided'
                            )),
    IsRead              BIT             NOT NULL DEFAULT 0,
    SentAt              DATETIME2(0)    NOT NULL DEFAULT GETDATE(),
    RelatedEntityType   NVARCHAR(50)    NULL,
    RelatedEntityID     INT             NULL,

    CONSTRAINT PK_Notifications PRIMARY KEY (NotificationID),

    CONSTRAINT FK_Notif_Recipient
        FOREIGN KEY (RecipientID) REFERENCES Users(UserID)
        ON DELETE CASCADE ON UPDATE NO ACTION
);

-- 11. PasswordResetTokens
CREATE TABLE PasswordResetTokens (
    TokenID     INT             NOT NULL IDENTITY(1,1),
    UserID      INT             NOT NULL,
    Token       NVARCHAR(256)   NOT NULL,
    ExpiresAt   DATETIME2(0)    NOT NULL,
    IsUsed      BIT             NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2(0)    NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_PasswordResetTokens  PRIMARY KEY (TokenID),
    CONSTRAINT UQ_ResetToken           UNIQUE (Token),

    CONSTRAINT FK_ResetToken_User
        FOREIGN KEY (UserID) REFERENCES Users(UserID)
        ON DELETE CASCADE ON UPDATE NO ACTION,

    CONSTRAINT CHK_ResetToken_Expiry
        CHECK (ExpiresAt > CreatedAt)
);

GO

-- ==============================================================================
-- 2. INDEXES
-- ==============================================================================

-- Users
CREATE INDEX IX_Users_Email    ON Users (Email)   WHERE IsActive = 1;
CREATE INDEX IX_Users_Role     ON Users (Role)    WHERE IsActive = 1;
CREATE INDEX IX_Users_Lockout  ON Users (LockoutUntil) WHERE LockoutUntil IS NOT NULL;

-- Rooms
CREATE INDEX IX_Rooms_IsActive ON Rooms (IsActive, RoomCode);

-- Equipments
CREATE INDEX IX_Equipments_Status      ON Equipments (Status) WHERE IsActive = 1;
CREATE INDEX IX_Equipments_Room        ON Equipments (CurrentRoomID) WHERE CurrentRoomID IS NOT NULL AND IsActive = 1;
CREATE INDEX IX_Equipments_Category    ON Equipments (CategoryID) WHERE IsActive = 1;
CREATE INDEX IX_Equipments_Warranty    ON Equipments (WarrantyExpiry) WHERE WarrantyExpiry IS NOT NULL AND Status != 'Disposed' AND IsActive = 1;
CREATE UNIQUE INDEX UIX_Equipments_AssetCode ON Equipments (AssetCode);

-- TransferHistories
CREATE INDEX IX_Transfer_Equipment ON TransferHistories (EquipmentID, TransferDate DESC);
CREATE INDEX IX_Transfer_ToRoom    ON TransferHistories (ToRoomID, TransferDate DESC);

-- EquipmentStatusLogs
CREATE INDEX IX_StatusLog_Equipment ON EquipmentStatusLogs (EquipmentID, ChangedAt DESC);
CREATE INDEX IX_StatusLog_ChangedBy ON EquipmentStatusLogs (ChangedBy, ChangedAt DESC);

-- IncidentReports
CREATE INDEX IX_Incident_Status      ON IncidentReports (Status, ReportedAt DESC);
CREATE INDEX IX_Incident_Equipment   ON IncidentReports (EquipmentID, Status);
CREATE INDEX IX_Incident_ReportedBy  ON IncidentReports (ReportedBy, ReportedAt DESC);
CREATE INDEX IX_Incident_Overdue     ON IncidentReports (DueDate) WHERE Status IN ('Pending', 'InProgress');

-- MaintenanceTickets
CREATE INDEX IX_Ticket_Status    ON MaintenanceTickets (Status, SentDate DESC);
CREATE INDEX IX_Ticket_Equipment ON MaintenanceTickets (EquipmentID, CreatedAt DESC);

-- DisposalRequests
CREATE INDEX IX_Disposal_Status    ON DisposalRequests (Status, ProposedAt DESC);
CREATE INDEX IX_Disposal_Equipment ON DisposalRequests (EquipmentID, Status);

-- Notifications
CREATE INDEX IX_Notif_Unread  ON Notifications (RecipientID, IsRead, SentAt DESC);
CREATE INDEX IX_Notif_User    ON Notifications (RecipientID, SentAt DESC);

-- PasswordResetTokens
CREATE INDEX IX_ResetToken_Token
    ON PasswordResetTokens (Token)
    WHERE IsUsed = 0;
CREATE INDEX IX_ResetToken_UserCreated
    ON PasswordResetTokens (UserID, CreatedAt DESC);
CREATE INDEX IX_ResetToken_Expiry
    ON PasswordResetTokens (ExpiresAt)
    WHERE IsUsed = 0;

GO

-- ==============================================================================
-- 3. VIEWS
-- ==============================================================================

CREATE VIEW vw_EquipmentDetails AS
SELECT
    e.EquipmentID,
    e.AssetCode,
    e.EquipmentName,
    e.Status,
    e.PurchaseDate,
    e.WarrantyExpiry,
    ec.CategoryName,
    r.RoomCode,
    r.RoomName,
    u.FullName AS CreatedByName
FROM Equipments e
LEFT JOIN EquipmentCategories ec ON e.CategoryID = ec.CategoryID
LEFT JOIN Rooms r                ON e.CurrentRoomID = r.RoomID
LEFT JOIN Users u                ON e.CreatedBy = u.UserID
WHERE e.IsActive = 1;
GO

CREATE VIEW vw_PendingIncidents AS
SELECT
    ir.IncidentID,
    ir.Status,
    ir.ReportedAt,
    ir.DueDate,
    ir.IsOverdue,
    e.AssetCode,
    e.EquipmentName,
    r.RoomCode,
    r.RoomName,
    u.FullName AS ReportedByName
FROM IncidentReports ir
JOIN Equipments e ON ir.EquipmentID = e.EquipmentID
JOIN Rooms r      ON ir.RoomID = r.RoomID
JOIN Users u      ON ir.ReportedBy = u.UserID
WHERE ir.Status IN ('Pending', 'InProgress') AND e.IsActive = 1;
GO

CREATE VIEW vw_DashboardStats AS
SELECT
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'InUse' AND IsActive = 1)               AS TotalInUse,
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'PendingRepair' AND IsActive = 1)       AS TotalPendingRepair,
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'UnderMaintenance' AND IsActive = 1)    AS TotalUnderMaintenance,
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'ProposedDisposal' AND IsActive = 1)    AS TotalProposedDisposal,
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'Disposed' AND IsActive = 1)            AS TotalDisposed,
    (SELECT COUNT(*) FROM IncidentReports WHERE Status = 'Pending')        AS OpenIncidents,
    (SELECT COUNT(*) FROM IncidentReports WHERE IsOverdue = 1)             AS OverdueIncidents,
    (SELECT COUNT(*) FROM DisposalRequests WHERE Status = 'Pending')       AS PendingDisposals;
GO

-- ==============================================================================
-- 4. STORED PROCEDURES
-- ==============================================================================

CREATE PROCEDURE sp_CreatePasswordResetToken
    @Email      NVARCHAR(256),
    @Token      NVARCHAR(256),
    @ExpiresAt  DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    -- Bước 1: Tìm user theo email
    DECLARE @UserID INT;
    SELECT @UserID = UserID
    FROM Users
    WHERE Email = @Email AND IsActive = 1;

    IF @UserID IS NULL
    BEGIN
        RAISERROR('Email không tồn tại trong hệ thống.', 16, 1);
        RETURN;
    END

    -- Bước 2: Kiểm tra Business Rule — tối đa 3 lần / 24 giờ
    DECLARE @RequestCount INT;
    SELECT @RequestCount = COUNT(*)
    FROM PasswordResetTokens
    WHERE UserID = @UserID
      AND CreatedAt > DATEADD(HOUR, -24, GETDATE());

    IF @RequestCount >= 3
    BEGIN
        RAISERROR('Đã vượt quá giới hạn 3 lần yêu cầu đặt lại mật khẩu trong 24 giờ.', 16, 1);
        RETURN;
    END

    -- Bước 3: Vô hiệu hóa các token cũ chưa dùng của user này
    UPDATE PasswordResetTokens
    SET IsUsed = 1
    WHERE UserID = @UserID AND IsUsed = 0;

    -- Bước 4: Tạo token mới
    INSERT INTO PasswordResetTokens (UserID, Token, ExpiresAt)
    VALUES (@UserID, @Token, @ExpiresAt);

    -- Trả về UserID và FullName để gửi email
    SELECT UserID, FullName, Email
    FROM Users
    WHERE UserID = @UserID;
END;
GO

CREATE PROCEDURE sp_ResetPassword
    @Token          NVARCHAR(256),
    @NewPasswordHash NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    -- Bước 1: Kiểm tra token hợp lệ (chưa dùng, chưa hết hạn)
    DECLARE @UserID INT;
    SELECT @UserID = UserID
    FROM PasswordResetTokens
    WHERE Token = @Token
      AND IsUsed = 0
      AND ExpiresAt > GETDATE();

    IF @UserID IS NULL
    BEGIN
        RAISERROR('Token không hợp lệ hoặc đã hết hạn.', 16, 1);
        RETURN;
    END

    -- Bước 2: Cập nhật mật khẩu mới
    UPDATE Users
    SET PasswordHash = @NewPasswordHash,
        UpdatedAt    = GETDATE(),
        FailedLoginCount = 0,
        LockoutUntil     = NULL
    WHERE UserID = @UserID;

    -- Bước 3: Đánh dấu token đã dùng
    UPDATE PasswordResetTokens
    SET IsUsed = 1
    WHERE Token = @Token;

    SELECT 'Password reset successfully.' AS Result;
END;
GO

-- ==============================================================================
-- 5. TRIGGERS
-- ==============================================================================

CREATE TRIGGER trg_Equipment_AfterUpdate
ON Equipments
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Ghi log khi Status thay đổi
    IF UPDATE(Status)
    BEGIN
        INSERT INTO EquipmentStatusLogs (EquipmentID, ChangedBy, OldStatus, NewStatus, FieldChanged, ChangedAt)
        SELECT
            i.EquipmentID,
            i.UpdatedBy,
            d.Status,   -- OldStatus
            i.Status,   -- NewStatus
            'Status',
            GETDATE()
        FROM inserted i
        JOIN deleted d ON i.EquipmentID = d.EquipmentID
        WHERE i.Status != d.Status;
    END

    -- Ghi log khi WarrantyExpiry thay đổi
    IF UPDATE(WarrantyExpiry)
    BEGIN
        INSERT INTO EquipmentStatusLogs (EquipmentID, ChangedBy, OldStatus, NewStatus, FieldChanged, OldValue, NewValue, ChangedAt)
        SELECT
            i.EquipmentID,
            i.UpdatedBy,
            i.Status,
            i.Status,
            'WarrantyExpiry',
            CONVERT(NVARCHAR, d.WarrantyExpiry, 23),
            CONVERT(NVARCHAR, i.WarrantyExpiry, 23),
            GETDATE()
        FROM inserted i
        JOIN deleted d ON i.EquipmentID = d.EquipmentID
        WHERE ISNULL(i.WarrantyExpiry, '1900-01-01') != ISNULL(d.WarrantyExpiry, '1900-01-01');
    END
END;
GO

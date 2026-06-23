# Physical ERD Design — Classroom Equipment Management System (CEMS)
**DBMS:** Microsoft SQL Server 2019+  
**Collation:** Vietnamese_CI_AS  
**Phiên bản:** v1.0  
**Tham chiếu Logical ERD:** ERD_Design.md v1.1  
**Ngày:** 2026-06-23

---

## 1. Mapping Logical → Physical (Kiểu dữ liệu)

| Kiểu Logical | Kiểu Physical (SQL Server) | Ghi chú |
|---|---|---|
| `Integer` (PK/FK) | `INT` | Auto-increment dùng `IDENTITY(1,1)` |
| `String` (short code) | `NVARCHAR(20)` | Mã phòng, mã người dùng |
| `String` (name) | `NVARCHAR(150)` | Họ tên, tên thiết bị |
| `String` (email) | `NVARCHAR(256)` | Theo chuẩn RFC 5321 |
| `String` (password hash) | `NVARCHAR(256)` | BCrypt hash output |
| `String` (url/path) | `NVARCHAR(500)` | URL ảnh, file path |
| `String` (enum/status) | `NVARCHAR(30)` | Trạng thái, vai trò |
| `String` (phone) | `NVARCHAR(20)` | Số điện thoại |
| `String` (short note) | `NVARCHAR(500)` | Lý do, ghi chú ngắn |
| `Text` (long content) | `NVARCHAR(MAX)` | Mô tả sự cố, ghi chú dài |
| `Boolean` | `BIT` | 0 = false, 1 = true |
| `DateTime` | `DATETIME2(0)` | Độ chính xác giây, tiết kiệm space |
| `Date` | `DATE` | Không cần giờ/phút/giây |
| `Decimal` (money) | `DECIMAL(15, 2)` | Đủ cho VNĐ và USD |

---

## 2. Schema vật lý từng bảng

### 2.1 Bảng `Users`

```sql
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
```

**Indexes:**
```sql
-- Tìm kiếm theo email khi đăng nhập (hot path)
CREATE INDEX IX_Users_Email    ON Users (Email)   WHERE IsActive = 1;
-- Lọc theo vai trò trong trang quản lý tài khoản
CREATE INDEX IX_Users_Role     ON Users (Role)    WHERE IsActive = 1;
-- Lọc tài khoản đang bị khóa
CREATE INDEX IX_Users_Lockout  ON Users (LockoutUntil) WHERE LockoutUntil IS NOT NULL;
```

---

### 2.2 Bảng `Rooms`

```sql
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
```

**Indexes:**
```sql
-- Danh sách phòng đang hoạt động (màn hình chọn phòng)
CREATE INDEX IX_Rooms_IsActive ON Rooms (IsActive, RoomCode);
```

---

### 2.3 Bảng `EquipmentCategories`

```sql
CREATE TABLE EquipmentCategories (
    CategoryID      INT             NOT NULL IDENTITY(1,1),
    CategoryName    NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(MAX)   NULL,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_EquipmentCategories PRIMARY KEY (CategoryID),
    CONSTRAINT UQ_EquipmentCategories_Name UNIQUE (CategoryName)
);
```

---

### 2.4 Bảng `Equipments` ⭐ (Trung tâm)

```sql
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
    Notes           NVARCHAR(MAX)   NULL,
    CreatedBy       INT             NOT NULL,
    UpdatedBy       INT             NULL,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME2(0)    NULL,

    CONSTRAINT PK_Equipments            PRIMARY KEY (EquipmentID),
    CONSTRAINT UQ_Equipments_AssetCode  UNIQUE (AssetCode),

    CONSTRAINT FK_Equipments_Category
        FOREIGN KEY (CategoryID) REFERENCES EquipmentCategories(CategoryID)
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Equipments_Room
        FOREIGN KEY (CurrentRoomID) REFERENCES Rooms(RoomID)
        ON DELETE SET NULL ON UPDATE CASCADE,

    CONSTRAINT FK_Equipments_CreatedBy
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Equipments_UpdatedBy
        FOREIGN KEY (UpdatedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
```

**Indexes:**
```sql
-- Lọc theo trạng thái (màn hình chính Technician)
CREATE INDEX IX_Equipments_Status      ON Equipments (Status);
-- Lọc theo phòng (Lecturer xem thiết bị trong phòng)
CREATE INDEX IX_Equipments_Room        ON Equipments (CurrentRoomID) WHERE CurrentRoomID IS NOT NULL;
-- Lọc theo loại
CREATE INDEX IX_Equipments_Category    ON Equipments (CategoryID);
-- Cảnh báo hết bảo hành (background job)
CREATE INDEX IX_Equipments_Warranty    ON Equipments (WarrantyExpiry) WHERE WarrantyExpiry IS NOT NULL AND Status != 'Disposed';
-- Tìm nhanh theo AssetCode
CREATE UNIQUE INDEX UIX_Equipments_AssetCode ON Equipments (AssetCode);
```

---

### 2.5 Bảng `TransferHistories`

```sql
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
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Transfer_FromRoom
        FOREIGN KEY (FromRoomID) REFERENCES Rooms(RoomID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Transfer_ToRoom
        FOREIGN KEY (ToRoomID) REFERENCES Rooms(RoomID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT FK_Transfer_TransferredBy
        FOREIGN KEY (TransferredBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT CHK_Transfer_Rooms
        CHECK (FromRoomID IS NULL OR FromRoomID != ToRoomID)
);
```

**Indexes:**
```sql
-- Xem lịch sử chuyển phòng của 1 thiết bị (sắp theo thời gian giảm)
CREATE INDEX IX_Transfer_Equipment ON TransferHistories (EquipmentID, TransferDate DESC);
-- Xem lịch sử theo phòng
CREATE INDEX IX_Transfer_ToRoom    ON TransferHistories (ToRoomID, TransferDate DESC);
```

---

### 2.6 Bảng `EquipmentStatusLogs`

```sql
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
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_StatusLog_ChangedBy
        FOREIGN KEY (ChangedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE CASCADE
);
```

**Indexes:**
```sql
-- Xem toàn bộ lịch sử của 1 thiết bị (sắp theo thời gian giảm)
CREATE INDEX IX_StatusLog_Equipment ON EquipmentStatusLogs (EquipmentID, ChangedAt DESC);
-- Xem ai đã thực hiện thay đổi
CREATE INDEX IX_StatusLog_ChangedBy ON EquipmentStatusLogs (ChangedBy, ChangedAt DESC);
```

---

### 2.7 Bảng `IncidentReports`

```sql
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
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Incident_Room
        FOREIGN KEY (RoomID) REFERENCES Rooms(RoomID)
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Incident_ReportedBy
        FOREIGN KEY (ReportedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Incident_AssignedTo
        FOREIGN KEY (AssignedTo) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT CHK_Incident_ResolvedAt
        CHECK (ResolvedAt IS NULL OR ResolvedAt >= ReportedAt)
);
```

**Indexes:**
```sql
-- Danh sách sự cố đang Pending (Technician xử lý)
CREATE INDEX IX_Incident_Status      ON IncidentReports (Status, ReportedAt DESC);
-- Sự cố của 1 thiết bị cụ thể
CREATE INDEX IX_Incident_Equipment   ON IncidentReports (EquipmentID, Status);
-- Sự cố do 1 Giảng viên gửi (Lecturer theo dõi)
CREATE INDEX IX_Incident_ReportedBy  ON IncidentReports (ReportedBy, ReportedAt DESC);
-- Phát hiện sự cố quá hạn (background job)
CREATE INDEX IX_Incident_Overdue     ON IncidentReports (DueDate) WHERE Status IN ('Pending', 'InProgress');
```

---

### 2.8 Bảng `MaintenanceTickets`

```sql
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
    CONSTRAINT UQ_Ticket_Incident    UNIQUE (IncidentID),   -- 1 sự cố chỉ có 1 phiếu

    CONSTRAINT FK_Ticket_Incident
        FOREIGN KEY (IncidentID) REFERENCES IncidentReports(IncidentID)
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Ticket_Equipment
        FOREIGN KEY (EquipmentID) REFERENCES Equipments(EquipmentID)
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Ticket_CreatedBy
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT CHK_Ticket_Dates
        CHECK (ExpectedReturnDate >= SentDate),

    CONSTRAINT CHK_Ticket_ActualReturn
        CHECK (ActualReturnDate IS NULL OR ActualReturnDate >= SentDate)
);
```

**Indexes:**
```sql
-- Phiếu theo trạng thái
CREATE INDEX IX_Ticket_Status    ON MaintenanceTickets (Status, SentDate DESC);
-- Phiếu của 1 thiết bị
CREATE INDEX IX_Ticket_Equipment ON MaintenanceTickets (EquipmentID, CreatedAt DESC);
```

---

### 2.9 Bảng `DisposalRequests`

```sql
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
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Disposal_ProposedBy
        FOREIGN KEY (ProposedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE CASCADE,

    CONSTRAINT FK_Disposal_ApprovedBy
        FOREIGN KEY (ApprovedBy) REFERENCES Users(UserID)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT CHK_Disposal_DecidedAt
        CHECK (DecidedAt IS NULL OR DecidedAt >= ProposedAt)
);
```

**Indexes:**
```sql
-- Danh sách chờ Admin phê duyệt
CREATE INDEX IX_Disposal_Status    ON DisposalRequests (Status, ProposedAt DESC);
-- Đề xuất của 1 thiết bị
CREATE INDEX IX_Disposal_Equipment ON DisposalRequests (EquipmentID, Status);
```

---

### 2.10 Bảng `Notifications`

```sql
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
        ON DELETE CASCADE ON UPDATE CASCADE
);
```

**Indexes:**
```sql
-- Thông báo chưa đọc của 1 user (badge count, notification bell)
CREATE INDEX IX_Notif_Unread  ON Notifications (RecipientID, IsRead, SentAt DESC);
-- Tất cả thông báo của 1 user
CREATE INDEX IX_Notif_User    ON Notifications (RecipientID, SentAt DESC);
```

---

### 2.11 Bảng `PasswordResetTokens` *(Quên mật khẩu)*

> Lưu token đặt lại mật khẩu tạm thời khi người dùng thực hiện `UC_ForgotPassword`. Hỗ trợ Business Rule: tối đa 3 lần yêu cầu trong 24 giờ.

```sql
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
        ON DELETE CASCADE ON UPDATE CASCADE,

    CONSTRAINT CHK_ResetToken_Expiry
        CHECK (ExpiresAt > CreatedAt)
);
```

**Indexes:**
```sql
-- Tra cứu token khi user bấm link reset (hot path)
CREATE INDEX IX_ResetToken_Token
    ON PasswordResetTokens (Token)
    WHERE IsUsed = 0;

-- Đếm số lần yêu cầu trong 24h (Business Rule: max 3 lần)
CREATE INDEX IX_ResetToken_UserCreated
    ON PasswordResetTokens (UserID, CreatedAt DESC);

-- Dọn dẹp token hết hạn (background cleanup job)
CREATE INDEX IX_ResetToken_Expiry
    ON PasswordResetTokens (ExpiresAt)
    WHERE IsUsed = 0;
```

**Stored Procedure — Tạo token mới (áp dụng Business Rule):**
```sql
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
```

**Stored Procedure — Xác thực token và đặt lại mật khẩu:**
```sql
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
```

---

## 3. Thứ tự tạo bảng (Dependency Order)

> Phải tạo bảng cha trước bảng con (theo FK dependency).

```
1.  Users                     (không phụ thuộc)
2.  Rooms                     (không phụ thuộc)
3.  EquipmentCategories       (không phụ thuộc)
4.  Equipments                (phụ thuộc: Users, Rooms, EquipmentCategories)
5.  TransferHistories         (phụ thuộc: Equipments, Rooms, Users)
6.  EquipmentStatusLogs       (phụ thuộc: Equipments, Users)
7.  IncidentReports           (phụ thuộc: Equipments, Rooms, Users)
8.  MaintenanceTickets        (phụ thuộc: IncidentReports, Equipments, Users)
9.  DisposalRequests          (phụ thuộc: Equipments, Users)
10. Notifications             (phụ thuộc: Users)
11. PasswordResetTokens       (phụ thuộc: Users)
```

---

## 4. Tổng hợp Constraints toàn hệ thống

| Bảng | CHECK Constraints | Mô tả |
|---|---|---|
| `Users` | `CHK_Users_Role` | Role chỉ nhận 3 giá trị hợp lệ |
| `Rooms` | `CHK_Rooms_Capacity` | Capacity > 0 |
| `Rooms` | `CHK_Rooms_RoomType` | RoomType chỉ nhận 4 giá trị |
| `Equipments` | `CHK_Equipments_Status` | Status theo state machine |
| `TransferHistories` | `CHK_Transfer_Rooms` | FromRoom ≠ ToRoom |
| `IncidentReports` | `CHK_Incident_Status` | Status theo 4 giá trị |
| `IncidentReports` | `CHK_Incident_ResolvedAt` | ResolvedAt ≥ ReportedAt |
| `MaintenanceTickets` | `UQ_Ticket_Incident` | 1 sự cố → tối đa 1 phiếu |
| `MaintenanceTickets` | `CHK_Ticket_Dates` | ExpectedReturn ≥ SentDate |
| `MaintenanceTickets` | `CHK_Ticket_EstCost` | Chi phí ≥ 0 |
| `DisposalRequests` | `CHK_Disposal_Status` | Status 3 giá trị |
| `DisposalRequests` | `CHK_Disposal_DecidedAt` | DecidedAt ≥ ProposedAt |
| `Notifications` | `CHK_Notif_Type` | Type 6 giá trị |
| `PasswordResetTokens` | `UQ_ResetToken` | Token là duy nhất toàn hệ thống |
| `PasswordResetTokens` | `CHK_ResetToken_Expiry` | ExpiresAt > CreatedAt |
| `PasswordResetTokens` | *(App logic)* | Tối đa 3 lần yêu cầu / 24h — kiểm tra qua `sp_CreatePasswordResetToken` |

---

## 5. Tổng hợp Index Strategy

| Bảng | Index | Mục đích | Loại |
|---|---|---|---|
| `Users` | `IX_Users_Email` | Đăng nhập | Filtered |
| `Users` | `IX_Users_Role` | Lọc theo vai trò | Filtered |
| `Equipments` | `IX_Equipments_Status` | Lọc thiết bị theo trạng thái | Non-clustered |
| `Equipments` | `IX_Equipments_Room` | Xem thiết bị trong phòng | Filtered |
| `Equipments` | `IX_Equipments_Warranty` | Background job cảnh báo bảo hành | Filtered |
| `TransferHistories` | `IX_Transfer_Equipment` | Lịch sử chuyển phòng theo thiết bị | Non-clustered |
| `EquipmentStatusLogs` | `IX_StatusLog_Equipment` | Audit trail theo thiết bị | Non-clustered |
| `IncidentReports` | `IX_Incident_Status` | Danh sách sự cố đang xử lý | Non-clustered |
| `IncidentReports` | `IX_Incident_Overdue` | Background job phát hiện quá hạn | Filtered |
| `Notifications` | `IX_Notif_Unread` | Badge count thông báo chưa đọc | Non-clustered |
| `PasswordResetTokens` | `IX_ResetToken_Token` | Tra cứu token khi user bấm link | Filtered (IsUsed=0) |
| `PasswordResetTokens` | `IX_ResetToken_UserCreated` | Đếm số lần yêu cầu / 24h | Non-clustered |
| `PasswordResetTokens` | `IX_ResetToken_Expiry` | Background cleanup token hết hạn | Filtered (IsUsed=0) |

---

## 6. Stored Procedures / Views khuyến nghị

### 6.1 View: Thiết bị kèm thông tin phòng và loại
```sql
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
LEFT JOIN Users u                ON e.CreatedBy = u.UserID;
```

### 6.2 View: Sự cố đang chờ xử lý (Technician Dashboard)
```sql
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
WHERE ir.Status IN ('Pending', 'InProgress');
```

### 6.3 View: Dashboard thống kê cho Admin
```sql
CREATE VIEW vw_DashboardStats AS
SELECT
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'InUse')               AS TotalInUse,
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'PendingRepair')       AS TotalPendingRepair,
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'UnderMaintenance')    AS TotalUnderMaintenance,
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'ProposedDisposal')    AS TotalProposedDisposal,
    (SELECT COUNT(*) FROM Equipments WHERE Status = 'Disposed')            AS TotalDisposed,
    (SELECT COUNT(*) FROM IncidentReports WHERE Status = 'Pending')        AS OpenIncidents,
    (SELECT COUNT(*) FROM IncidentReports WHERE IsOverdue = 1)             AS OverdueIncidents,
    (SELECT COUNT(*) FROM DisposalRequests WHERE Status = 'Pending')       AS PendingDisposals;
```

---

## 7. Trigger khuyến nghị

### 7.1 Auto-insert EquipmentStatusLog khi Equipment thay đổi
```sql
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
```

### 7.2 Auto-detect Overdue Incidents (có thể dùng SQL Agent Job)
```sql
-- Chạy mỗi ngày lúc 7:00 AM qua SQL Server Agent
UPDATE IncidentReports
SET IsOverdue = 1
WHERE Status IN ('Pending', 'InProgress')
  AND DueDate < GETDATE()
  AND IsOverdue = 0;
```

---

## 8. Đối chiếu với Checklist_LogicalERD

| Hạng mục Checklist | Trạng thái Physical ERD |
|---|---|
| Kiểu dữ liệu cụ thể DBMS | ✅ SQL Server: `NVARCHAR`, `INT`, `BIT`, `DECIMAL(15,2)`, `DATETIME2(0)` |
| CHECK Constraints đầy đủ | ✅ 13 CHECK constraints trên toàn hệ thống |
| UNIQUE Constraints | ✅ `AssetCode`, `Email`, `UserCode`, `RoomCode`, `CategoryName`, `UQ_Ticket_Incident` |
| Referential Integrity (ON DELETE/UPDATE) | ✅ Ghi rõ từng FK theo Logical ERD Section 3 |
| Indexes cho query hiệu quả | ✅ 11 indexes, ưu tiên Filtered Index |
| Thứ tự tạo bảng | ✅ Dependency order 10 bảng |
| Views hỗ trợ nghiệp vụ | ✅ 3 views: EquipmentDetails, PendingIncidents, DashboardStats |
| Trigger audit trail | ✅ trg_Equipment_AfterUpdate |

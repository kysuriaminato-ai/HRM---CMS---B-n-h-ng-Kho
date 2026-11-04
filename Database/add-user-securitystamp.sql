/*
  Thêm cột dbo.Users.SecurityStamp cho database ĐÃ CÓ DỮ LIỆU, trước khi chạy bản ứng dụng này.
  - Chạy một lần trên database thật (sao lưu trước). Chạy lại vẫn an toàn (idempotent).
  - Chạy trong transaction: nếu lỗi thì không để lại trạng thái dở dang.
  - Database mới không cần script này: migration baseline đã tạo sẵn cột này.
  - Lý do cần script: DbInitializer đánh dấu baseline là đã áp dụng cho DB cũ mà không chạy lại,
    nên cột phải có sẵn trước khi ứng dụng khởi động.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Users', N'SecurityStamp') IS NULL
BEGIN
    ALTER TABLE dbo.Users
        ADD SecurityStamp UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Users_SecurityStamp DEFAULT (NEWID());
    PRINT N'Đã thêm cột dbo.Users.SecurityStamp.';
END
ELSE
BEGIN
    PRINT N'Cột dbo.Users.SecurityStamp đã tồn tại, bỏ qua.';
END

COMMIT TRANSACTION;

-- =====================================================================
-- Sửa lỗi đăng ký: "Violation of UNIQUE KEY ... duplicate key (<NULL>)" trên dbo.Users
-- Nguyên nhân: Users.EmployeeId có ràng buộc UNIQUE thường; SQL Server chỉ cho 1 dòng NULL.
-- Cách sửa: bỏ mọi UNIQUE chỉ trên EmployeeId, tạo UNIQUE INDEX có lọc
--           (WHERE EmployeeId IS NOT NULL) đặt tên IX_Users_EmployeeId,
--           đúng với khai báo trong AppDbContext.OnModelCreating.
-- Cách chạy: trong SSMS chọn đúng database ERP_HRM_CMS_DB (không để master), rồi chạy toàn bộ file.
-- An toàn khi chạy nhiều lần. Toàn bộ thay đổi nằm trong một transaction.
-- =====================================================================

IF DB_NAME() = N'master'
BEGIN
    RAISERROR(N'Hãy chọn đúng database ERP_HRM_CMS_DB trước khi chạy script này.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    RAISERROR(N'Không tìm thấy bảng dbo.Users trong database đang chọn.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Kiểm tra: không có EmployeeId trùng (khác NULL) thì mới tạo được unique index
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE EmployeeId IS NOT NULL GROUP BY EmployeeId HAVING COUNT(*) > 1)
        THROW 50001, N'Có EmployeeId bị trùng (khác NULL) trong dbo.Users. Hãy xử lý dữ liệu trước khi chạy lại.', 1;

    -- Bước 1: xóa mọi UNIQUE constraint / UNIQUE index chỉ gồm đúng một cột EmployeeId.
    -- Không đụng vào khóa chính và các chỉ mục nhiều cột.
    DECLARE @sql NVARCHAR(MAX) = N'';

    SELECT @sql = @sql +
        CASE WHEN i.is_unique_constraint = 1
             THEN N'ALTER TABLE dbo.Users DROP CONSTRAINT ' + QUOTENAME(i.name) + N';'
             ELSE N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.Users;'
        END
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'dbo.Users', N'U')
      AND i.is_unique = 1
      AND i.is_primary_key = 0
      AND (SELECT COUNT(*)
           FROM sys.index_columns ic
           WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0) = 1
      AND EXISTS (SELECT 1
                  FROM sys.index_columns ic
                  JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                    AND ic.is_included_column = 0 AND c.name = N'EmployeeId');

    IF @sql <> N'' EXEC sp_executesql @sql;

    -- Bước 2: tạo UNIQUE INDEX có lọc, đúng tên với model EF
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE object_id = OBJECT_ID(N'dbo.Users', N'U') AND name = N'IX_Users_EmployeeId')
        CREATE UNIQUE NONCLUSTERED INDEX IX_Users_EmployeeId
            ON dbo.Users (EmployeeId)
            WHERE EmployeeId IS NOT NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Kiểm tra kết quả: đúng 1 dòng IX_Users_EmployeeId, is_unique = 1, filter_definition = ([EmployeeId] IS NOT NULL)
SELECT name, is_unique, is_unique_constraint, filter_definition
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.Users', N'U') AND name = N'IX_Users_EmployeeId';

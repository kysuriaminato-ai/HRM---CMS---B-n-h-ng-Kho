-- =====================================================================
-- Vô hiệu hóa các tài khoản demo trong SQLQuery1.sql (không xóa dữ liệu).
-- Giữ lại: admin, quantritong.
-- Chạy trên ERP_HRM_CMS_DB. Có thể chạy lại nhiều lần an toàn.
-- Muốn bật lại một tài khoản: UPDATE dbo.Users SET IsActive = 1 WHERE Username = N'...';
-- =====================================================================

IF DB_NAME() <> N'ERP_HRM_CMS_DB'
BEGIN
    RAISERROR(N'Script này chỉ chạy trên database ERP_HRM_CMS_DB. Hãy chọn đúng database trước.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE dbo.Users
    SET IsActive = 0
    WHERE Username IN (
        N'superadmin', N'adminmenu', N'admincontent', N'qtvp', N'saledirector', N'salestaff'
    )
    AND IsActive = 1;

    -- Đảm bảo vẫn còn ít nhất một superadmin đang hoạt động
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE IsSuperAdmin = 1 AND IsActive = 1)
        THROW 50002, N'Không còn superadmin nào đang hoạt động. Hủy thay đổi.', 1;

    COMMIT TRANSACTION;

    SELECT Username, IsActive, IsSuperAdmin
    FROM dbo.Users
    ORDER BY Username;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

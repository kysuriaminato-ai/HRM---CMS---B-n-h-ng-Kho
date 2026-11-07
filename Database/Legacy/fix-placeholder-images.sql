-- =====================================================================
-- [NGUỒN CHUẨN LEGACY] Database/Legacy/fix-placeholder-images.sql
-- (Đã hợp nhất từ bản trùng ở thư mục gốc fix-placeholder-images.sql trong Phiên A).
-- Lưu ý: Nguồn chuẩn schema & seed hiện hành nằm tại:
--   1. EF Core Migrations: src/ERP.Infrastructure/Migrations/ + Data/DbInitializer.cs
--   2. Script DBA chuẩn:   db/scripts/ (xem db/README.md)
-- Script này chỉ dùng để cập nhật ảnh placeholder cục bộ cho dữ liệu cũ đã có trong DB.
-- Chạy trên ERP_HRM_CMS_DB. Chạy lại nhiều lần vẫn an toàn (idempotent).
-- =====================================================================

IF DB_NAME() <> N'ERP_HRM_CMS_DB'
BEGIN
    RAISERROR(N'Script này chỉ chạy trên database ERP_HRM_CMS_DB. Hãy chọn đúng database trước.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @banner INT, @content INT;

    UPDATE dbo.Banners
    SET ImageUrl = CASE WHEN Id % 2 = 1 THEN '/images/banner-1.svg' ELSE '/images/banner-2.svg' END
    WHERE ImageUrl LIKE 'https://images.unsplash.com/%'
       OR ImageUrl LIKE '/uploads/banners/%'
       OR ImageUrl LIKE '/uploads/%';
    SET @banner = @@ROWCOUNT;

    UPDATE dbo.Contents
    SET ThumbnailUrl = CASE WHEN Id % 2 = 1 THEN '/images/news-1.svg' ELSE '/images/news-2.svg' END
    WHERE ThumbnailUrl LIKE 'https://images.unsplash.com/%'
       OR ThumbnailUrl LIKE '/uploads/contents/%'
       OR ThumbnailUrl LIKE '/uploads/%';
    SET @content = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT N'Banners đã cập nhật' AS Bang, @banner AS SoDong
    UNION ALL
    SELECT N'Contents đã cập nhật', @content;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

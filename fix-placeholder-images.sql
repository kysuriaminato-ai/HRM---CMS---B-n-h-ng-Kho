-- =====================================================================
-- Cập nhật ảnh placeholder cục bộ cho dữ liệu đã có trong DB
-- (seed chỉ chạy khi bảng còn trống nên không tự cập nhật được).
-- Chỉ đổi các URL unsplash và /uploads/ (không tồn tại) sang file SVG có sẵn trong wwwroot/images.
-- Chạy trên ERP_HRM_CMS_DB. Chạy lại nhiều lần vẫn an toàn.
-- =====================================================================

IF DB_NAME() <> N'ERP_HRM_CMS_DB'
BEGIN
    RAISERROR(N'Script này chỉ chạy trên database ERP_HRM_CMS_DB. Hãy chọn đúng database trước.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @banner INT, @content INT;

    UPDATE Banners
    SET ImageUrl = CASE WHEN Id % 2 = 1 THEN '/images/banner-1.svg' ELSE '/images/banner-2.svg' END
    WHERE ImageUrl LIKE 'https://images.unsplash.com/%' OR ImageUrl LIKE '/uploads/banners/%';
    SET @banner = @@ROWCOUNT;

    UPDATE Contents
    SET ThumbnailUrl = CASE WHEN Id % 2 = 1 THEN '/images/news-1.svg' ELSE '/images/news-2.svg' END
    WHERE ThumbnailUrl LIKE 'https://images.unsplash.com/%' OR ThumbnailUrl LIKE '/uploads/contents/%';
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

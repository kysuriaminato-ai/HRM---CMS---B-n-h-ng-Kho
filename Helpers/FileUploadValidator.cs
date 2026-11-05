using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace ERP_HRM_CMS.Helpers
{
    public static class FileUploadValidator
    {
        // PDF magic bytes: %PDF (0x25, 0x50, 0x44, 0x46)
        private static readonly byte[] PdfMagicBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        // DOCX (ZIP archive) magic bytes: PK\x03\x04 (0x50, 0x4B, 0x03, 0x04)
        private static readonly byte[] DocxMagicBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };

        public const long MaxCvFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public static (bool IsValid, string? ErrorMessage) ValidateCvFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "Vui lòng chọn tập tin hồ sơ ứng tuyển.");
            }

            if (file.Length > MaxCvFileSizeBytes)
            {
                return (false, "Dung lượng tập tin vượt quá giới hạn cho phép (tối đa 10MB).");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf" && extension != ".docx")
            {
                return (false, "Chỉ chấp nhận định dạng hồ sơ .pdf hoặc .docx.");
            }

            // Kiểm tra Magic Bytes
            using var stream = file.OpenReadStream();
            if (stream.Length < 4)
            {
                return (false, "Tập tin không hợp lệ hoặc bị hỏng.");
            }

            byte[] headerBytes = new byte[4];
            int bytesRead = stream.Read(headerBytes, 0, 4);
            if (bytesRead < 4)
            {
                return (false, "Không thể đọc nội dung tập tin.");
            }

            if (extension == ".pdf")
            {
                if (!headerBytes.SequenceEqual(PdfMagicBytes))
                {
                    return (false, "Nội dung tập tin không đúng định dạng PDF chuẩn (nghi ngờ giả mạo phần mở rộng).");
                }
            }
            else if (extension == ".docx")
            {
                if (!headerBytes.SequenceEqual(DocxMagicBytes))
                {
                    return (false, "Nội dung tập tin không đúng định dạng Word DOCX chuẩn (nghi ngờ giả mạo phần mở rộng).");
                }
            }

            return (true, null);
        }
    }
}


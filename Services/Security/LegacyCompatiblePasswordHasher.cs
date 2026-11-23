using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace ERP_HRM_CMS.Services.Security
{
    /// <summary>
    /// Bộ băm và kiểm chứng mật khẩu tương thích ngược cho <see cref="AppUser"/> (ADR-004 mục 2.2):
    /// 1. Nhận diện và kiểm chứng chuẩn ASP.NET Core Identity v3 PBKDF2-HMAC-SHA512 (bắt đầu bằng byte 0x01 ở dạng Base64).
    /// 2. Nhận diện và kiểm chứng chuẩn PBKDF2-SHA256 cũ (<see cref="MatKhauHelper"/>, 100.000 vòng + cột <see cref="User.PasswordSalt"/>).
    /// 3. Nhận diện và kiểm chứng chuẩn SHA-256 hex 64 ký tự cũ (<see cref="MatKhauHelper.IsLegacyHash"/>).
    /// Khi đăng nhập thành công với bất kỳ hash cũ nào (PBKDF2 cũ hoặc SHA-256 cũ), trả về
    /// <see cref="PasswordVerificationResult.SuccessRehashNeeded"/> để tự động nâng cấp lên chuẩn Identity v3 mới.
    /// </summary>
    public sealed class LegacyCompatiblePasswordHasher : IPasswordHasher<User>
    {
        private readonly PasswordHasher<User> _identityHasher = new();

        public string HashPassword(User user, string password)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrEmpty(password);

            // Đảm bảo cột PasswordSalt (NOT NULL, MaxLength 100) luôn có giá trị hợp lệ khi tạo/đổi mật khẩu
            if (string.IsNullOrEmpty(user.PasswordSalt))
            {
                user.PasswordSalt = MatKhauHelper.GenerateSalt();
            }

            return _identityHasher.HashPassword(user, password);
        }

        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // 1. Kiểm tra nếu là hash SHA-256 hex 64 ký tự cũ
            if (MatKhauHelper.IsLegacyHash(hashedPassword))
            {
                string salt = user?.PasswordSalt ?? string.Empty;
                // MatKhauHelper.Verify yêu cầu storedSalt không rỗng; nếu bản ghi SHA-256 cũ có salt rỗng thì dùng chuỗi tạm để kiểm tra nhánh không salt
                string effectiveSalt = string.IsNullOrEmpty(salt) ? "_" : salt;
                if (MatKhauHelper.Verify(providedPassword, hashedPassword, effectiveSalt))
                {
                    return PasswordVerificationResult.SuccessRehashNeeded;
                }
                return PasswordVerificationResult.Failed;
            }

            // 2. Kiểm tra nếu là định dạng ASP.NET Core Identity v2 (0x00) hoặc v3 (0x01)
            if (IsIdentityFormatHash(hashedPassword))
            {
                var identityResult = _identityHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
                if (identityResult != PasswordVerificationResult.Failed)
                {
                    return identityResult;
                }
            }

            // 3. Kiểm tra chuẩn PBKDF2-SHA256 cũ của MatKhauHelper (32 byte Base64 + cột PasswordSalt)
            if (user != null && !string.IsNullOrEmpty(user.PasswordSalt))
            {
                if (MatKhauHelper.Verify(providedPassword, hashedPassword, user.PasswordSalt))
                {
                    return PasswordVerificationResult.SuccessRehashNeeded;
                }
            }

            return PasswordVerificationResult.Failed;
        }

        /// <summary>
        /// Kiểm tra nhanh xem chuỗi Base64 có phải định dạng chuẩn của ASP.NET Core Identity PasswordHasher hay không:
        /// - Identity v3: byte đầu = 0x01, tổng độ dài >= 13 + 16 + 32 = 61 byte.
        /// - Identity v2: byte đầu = 0x00, tổng độ dài = 1 + 16 + 32 = 49 byte.
        /// (Khác với PBKDF2 cũ của MatKhauHelper vốn chỉ có đúng 32 byte = 44 ký tự Base64).
        /// </summary>
        public static bool IsIdentityFormatHash(string? hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword) || MatKhauHelper.IsLegacyHash(hashedPassword))
            {
                return false;
            }

            try
            {
                byte[] decoded = Convert.FromBase64String(hashedPassword);
                if (decoded.Length >= 61 && decoded[0] == 0x01)
                {
                    return true;
                }
                if (decoded.Length == 49 && decoded[0] == 0x00)
                {
                    return true;
                }
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}


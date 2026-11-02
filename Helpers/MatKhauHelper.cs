using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Băm và kiểm tra mật khẩu.
    /// - Tài khoản mới: PBKDF2-SHA256, salt ngẫu nhiên 16 byte, 100.000 vòng lặp.
    /// - Tài khoản cũ (SHA-256 hex 64 ký tự): vẫn kiểm tra được và được nâng cấp khi đăng nhập thành công.
    /// Không còn mật khẩu dùng chung cho mọi tài khoản.
    /// </summary>
    public static class MatKhauHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static string GenerateSalt()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));
        }

        public static string HashPassword(string rawPassword, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(rawPassword, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSize);
            return Convert.ToBase64String(hash);
        }

        public static bool IsLegacyHash(string? storedHash)
        {
            return !string.IsNullOrEmpty(storedHash) && storedHash.Length == 64 && storedHash.All(Uri.IsHexDigit);
        }

        public static bool Verify(string? inputPassword, string? storedHash, string? storedSalt)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
            {
                return false;
            }

            try
            {
                if (IsLegacyHash(storedHash))
                {
                    string legacyStored = storedHash.ToUpperInvariant();
                    return SoSanhAnToan(LegacySha256Hex(inputPassword), legacyStored)
                        || SoSanhAnToan(LegacySha256Hex(inputPassword + storedSalt), legacyStored);
                }

                return SoSanhAnToan(HashPassword(inputPassword, storedSalt), storedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>Trả về thông báo lỗi nếu mật khẩu chưa đủ mạnh, ngược lại trả về null.</summary>
        public static string? KiemTraDoManh(string? matKhau)
        {
            if (string.IsNullOrEmpty(matKhau) || matKhau.Length < 8)
            {
                return "Mật khẩu phải có ít nhất 8 ký tự.";
            }

            if (!matKhau.Any(char.IsLetter) || !matKhau.Any(char.IsDigit))
            {
                return "Mật khẩu phải gồm cả chữ và số.";
            }

            return null;
        }

        private static string LegacySha256Hex(string input)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        }

        private static bool SoSanhAnToan(string a, string b)
        {
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
        }
    }
}

using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Băm, kiểm tra và sinh mật khẩu an toàn mật mã học.
    /// - Tài khoản mới: PBKDF2-SHA256, salt ngẫu nhiên 16 byte, 100.000 vòng lặp.
    /// - Tài khoản cũ (SHA-256 hex 64 ký tự): vẫn kiểm tra được và được nâng cấp khi đăng nhập thành công.
    /// - Hỗ trợ băm giả lập hằng thời gian (VerifyDummy) chống dò tên đăng nhập qua timing side-channel.
    /// </summary>
    public static class MatKhauHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
        private const string DigitChars = "23456789";
        private const string SpecialChars = "!@#$%&*?";
        private const string AllSafeChars = UpperChars + LowerChars + DigitChars + SpecialChars;

        private static readonly string DummySalt = GenerateSalt();
        private static readonly string DummyHash = HashPassword("DummyTimingProtection#2026", DummySalt);

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

        /// <summary>
        /// Sinh mật khẩu tạm thời an toàn bằng CSPRNG (RandomNumberGenerator), luôn chứa chữ hoa, chữ thường, chữ số và ký tự đặc biệt.
        /// </summary>
        public static string GenerateTemporaryPassword(int length = 14)
        {
            if (length < 10) length = 10;

            var chars = new char[length];
            chars[0] = UpperChars[RandomNumberGenerator.GetInt32(UpperChars.Length)];
            chars[1] = LowerChars[RandomNumberGenerator.GetInt32(LowerChars.Length)];
            chars[2] = DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)];
            chars[3] = SpecialChars[RandomNumberGenerator.GetInt32(SpecialChars.Length)];

            for (int i = 4; i < length; i++)
            {
                chars[i] = AllSafeChars[RandomNumberGenerator.GetInt32(AllSafeChars.Length)];
            }

            // Fisher-Yates shuffle bằng CSPRNG
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }

        /// <summary>
        /// Thực hiện băm giả lập khi không tìm thấy username để thời gian phản hồi đồng nhất (chống timing attack S8a).
        /// </summary>
        public static void VerifyDummy(string? inputPassword)
        {
            _ = Verify(inputPassword ?? string.Empty, DummyHash, DummySalt);
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

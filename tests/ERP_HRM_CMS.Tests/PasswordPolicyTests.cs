using ERP_HRM_CMS.Helpers;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class PasswordPolicyTests
    {
        [Theory]
        [InlineData("123456")]
        [InlineData("abc123")]
        [InlineData("")]
        [InlineData(null)]
        public void WeakPassword_IsRejected(string? password)
        {
            Assert.NotNull(MatKhauHelper.KiemTraDoManh(password));
        }

        [Fact]
        public void StrongPassword_IsAccepted()
        {
            Assert.Null(MatKhauHelper.KiemTraDoManh("Abcd1234"));
        }

        [Fact]
        public void Hash_Verifies_OnlyWithCorrectPassword()
        {
            var salt = MatKhauHelper.GenerateSalt();
            var hash = MatKhauHelper.HashPassword("Abcd1234", salt);

            Assert.True(MatKhauHelper.Verify("Abcd1234", hash, salt));
            Assert.False(MatKhauHelper.Verify("Abcd1235", hash, salt));
        }
    }
}

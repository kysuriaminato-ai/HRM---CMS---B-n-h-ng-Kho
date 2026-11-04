using ERP_HRM_CMS.Data;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class SeedGuardTests
    {
        [Fact]
        public void First_Run_Without_Admin_Password_Stops_Startup()
        {
            Assert.Throws<InvalidOperationException>(() =>
                DbInitializer.EnsureInitialAdminPasswordConfigured(accountMissing: true, initialAdminPassword: null));
        }

        [Fact]
        public void Whitespace_Admin_Password_Counts_As_Missing()
        {
            Assert.Throws<InvalidOperationException>(() =>
                DbInitializer.EnsureInitialAdminPasswordConfigured(accountMissing: true, initialAdminPassword: "   "));
        }

        [Fact]
        public void Existing_Admin_Accounts_Do_Not_Need_The_Password()
        {
            var error = Record.Exception(() =>
                DbInitializer.EnsureInitialAdminPasswordConfigured(accountMissing: false, initialAdminPassword: null));
            Assert.Null(error);
        }

        [Fact]
        public void Configured_Admin_Password_Is_Accepted()
        {
            var error = Record.Exception(() =>
                DbInitializer.EnsureInitialAdminPasswordConfigured(accountMissing: true, initialAdminPassword: "Abc@123456"));
            Assert.Null(error);
        }
    }
}

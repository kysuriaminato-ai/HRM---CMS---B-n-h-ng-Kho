using ERP_HRM_CMS.Services.Payroll;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Kiểm thử việc chọn phiên bản tham số theo ngày hiệu lực và cảnh báo quy định chưa xác nhận.
    /// </summary>
    public class PayrollRuleResolverTests
    {
        [Fact]
        public void Resolve_Uses_The_Version_Effective_At_The_Start_Of_The_Period()
        {
            var options = ValidOptions();

            var before = PayrollRuleResolver.Resolve(options, new DateTime(2026, 6, 1));
            var after = PayrollRuleResolver.Resolve(options, new DateTime(2026, 7, 1));

            Assert.Equal(2_340_000m, before.StatutoryBaseSalary);
            Assert.Equal(2_530_000m, after.StatutoryBaseSalary);
        }

        [Fact]
        public void Resolve_Picks_The_Personal_Deduction_For_2026()
        {
            var rules = PayrollRuleResolver.Resolve(ValidOptions(), new DateTime(2026, 3, 1));

            Assert.Equal(15_500_000m, rules.PersonalDeduction);
            Assert.Equal(6_200_000m, rules.DependentDeduction);
        }

        [Fact]
        public void Resolve_Uses_Older_Version_For_Periods_Before_2026()
        {
            var rules = PayrollRuleResolver.Resolve(ValidOptions(), new DateTime(2025, 12, 1));

            Assert.Equal(11_000_000m, rules.PersonalDeduction);
            Assert.Equal(4_400_000m, rules.DependentDeduction);
        }

        [Fact]
        public void Resolve_Throws_When_No_Version_Is_Effective_Yet()
        {
            // Kỳ tháng 6/2024 đứng trước mọi phiên bản lương cơ sở (bắt đầu 01/07/2024)
            var ex = Assert.Throws<InvalidOperationException>(
                () => PayrollRuleResolver.Resolve(ValidOptions(), new DateTime(2024, 6, 1)));

            Assert.Contains("Lương cơ sở", ex.Message);
        }

        [Fact]
        public void Resolve_Lists_Rules_That_Still_Need_Confirmation()
        {
            var rules = PayrollRuleResolver.Resolve(ValidOptions(), new DateTime(2026, 7, 1));

            Assert.Contains(rules.PendingConfirmations, p => p.StartsWith("Lương cơ sở"));
            Assert.Contains(rules.PendingConfirmations, p => p.StartsWith("Trần đóng bảo hiểm"));
        }

        [Fact]
        public void Resolve_Lists_Nothing_When_All_Rules_Are_Confirmed()
        {
            var options = ValidOptions();
            options.BaseSalary = new List<MoneyRule>
            {
                new() { EffectiveFrom = new DateTime(2024, 7, 1), Amount = 2_340_000m, Source = "Nghị định 73/2024/NĐ-CP" }
            };
            options.InsuranceCap.NeedsConfirmation = false;
            options.LatePenalty.NeedsConfirmation = false;
            options.Overtime.NeedsConfirmation = false;

            var rules = PayrollRuleResolver.Resolve(options, new DateTime(2026, 1, 1));

            Assert.DoesNotContain(rules.PendingConfirmations, p => p.StartsWith("Lương cơ sở"));
            Assert.DoesNotContain(rules.PendingConfirmations, p => p.StartsWith("Trần đóng bảo hiểm"));
        }

        [Fact]
        public void Resolve_Rejects_Brackets_That_Are_Not_Ascending()
        {
            var options = ValidOptions();
            options.PitSchedules[0].Brackets = new List<PitBracketOption>
            {
                new() { UpTo = 10_000_000m, Rate = 0.05m },
                new() { UpTo = 5_000_000m, Rate = 0.10m },
                new() { UpTo = null, Rate = 0.35m }
            };

            Assert.Throws<InvalidOperationException>(() => PayrollRuleResolver.Resolve(options, new DateTime(2026, 7, 1)));
        }

        [Fact]
        public void Resolve_Rejects_Brackets_Without_An_Open_Top_Bracket()
        {
            var options = ValidOptions();
            options.PitSchedules[0].Brackets = new List<PitBracketOption>
            {
                new() { UpTo = 5_000_000m, Rate = 0.05m },
                new() { UpTo = 10_000_000m, Rate = 0.10m }
            };

            Assert.Throws<InvalidOperationException>(() => PayrollRuleResolver.Resolve(options, new DateTime(2026, 7, 1)));
        }

        [Fact]
        public void Resolve_Rejects_Non_Positive_Insurance_Cap_Multiplier()
        {
            var options = ValidOptions();
            options.InsuranceCap.Multiplier = 0m;

            Assert.Throws<InvalidOperationException>(() => PayrollRuleResolver.Resolve(options, new DateTime(2026, 7, 1)));
        }

        [Fact]
        public void Default_Options_Have_No_Versions_So_Calculation_Stops()
        {
            // Cấu hình rỗng không được phép âm thầm dùng số mặc định: phải báo lỗi.
            Assert.Throws<InvalidOperationException>(
                () => PayrollRuleResolver.Resolve(new PayrollOptions(), new DateTime(2026, 7, 1)));
        }

        /// <summary>Cấu hình hợp lệ tương tự appsettings.json.</summary>
        private static PayrollOptions ValidOptions() => new()
        {
            BaseSalary = new List<MoneyRule>
            {
                new() { EffectiveFrom = new DateTime(2024, 7, 1), Amount = 2_340_000m, Source = "Nghị định 73/2024/NĐ-CP" },
                new() { EffectiveFrom = new DateTime(2026, 7, 1), Amount = 2_530_000m, Source = "Nghị định 161/2026/NĐ-CP", NeedsConfirmation = true, Note = "TODO" }
            },
            PersonalDeduction = new List<MoneyRule>
            {
                new() { EffectiveFrom = new DateTime(2020, 7, 1), Amount = 11_000_000m, Source = "NQ 954/2020" },
                new() { EffectiveFrom = new DateTime(2026, 1, 1), Amount = 15_500_000m, Source = "NQ 110/2025" }
            },
            DependentDeduction = new List<MoneyRule>
            {
                new() { EffectiveFrom = new DateTime(2020, 7, 1), Amount = 4_400_000m, Source = "NQ 954/2020" },
                new() { EffectiveFrom = new DateTime(2026, 1, 1), Amount = 6_200_000m, Source = "NQ 110/2025" }
            },
            InsuranceRates = new List<InsuranceRateRule>
            {
                new()
                {
                    EffectiveFrom = new DateTime(2025, 7, 1),
                    Source = "Luật BHXH 2024",
                    EmployeeSocial = 0.08m,
                    EmployeeHealth = 0.015m,
                    EmployeeUnemployment = 0.01m,
                    EmployerPension = 0.14m,
                    EmployerSickMaternity = 0.03m,
                    EmployerHealth = 0.03m,
                    EmployerUnemployment = 0.01m,
                    EmployerAccident = 0.005m
                }
            },
            PitSchedules = new List<PitScheduleRule>
            {
                new()
                {
                    EffectiveFrom = new DateTime(2013, 7, 1),
                    Source = "Luật TNCN",
                    Brackets = new List<PitBracketOption>
                    {
                        new() { UpTo = 5_000_000m, Rate = 0.05m },
                        new() { UpTo = 10_000_000m, Rate = 0.10m },
                        new() { UpTo = null, Rate = 0.35m }
                    }
                }
            },
            InsuranceCap = new InsuranceCapRule { Multiplier = 20m, Source = "Luật BHXH 2024 Điều 31", NeedsConfirmation = true, Note = "TODO" },
            LatePenalty = new LatePenaltyRule { BlockMinutes = 15, AmountPerBlock = 50_000m, Source = "Nội bộ" },
            Overtime = new OvertimeRule { WeekdayMultiplier = 1.5m, HoursPerDay = 8m, Source = "BLLĐ 2019 Điều 98" }
        };
    }
}

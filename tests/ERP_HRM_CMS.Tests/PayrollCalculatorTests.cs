using ERP_HRM_CMS.Services.Payroll;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Kiểm thử công thức lương thuần: thuế lũy tiến, BHXH có trần, phạt đi muộn, tăng ca, nghỉ không lương.
    /// Không cần database.
    /// </summary>
    public class PayrollCalculatorTests
    {
        // ===== Thuế lũy tiến từng bậc =====

        [Fact]
        public void ProgressivePit_Returns_Zero_For_Non_Positive_Income()
        {
            var brackets = PayrollTestData.Brackets();
            Assert.Equal(0m, PayrollCalculator.ProgressivePit(0m, brackets));
            Assert.Equal(0m, PayrollCalculator.ProgressivePit(-500_000m, brackets));
        }

        [Fact]
        public void ProgressivePit_Matches_Each_Bracket_Boundary()
        {
            var brackets = PayrollTestData.Brackets();

            Assert.Equal(250_000m, PayrollCalculator.ProgressivePit(5_000_000m, brackets));    // 5M × 5%
            Assert.Equal(750_000m, PayrollCalculator.ProgressivePit(10_000_000m, brackets));   // 0,25M + 0,5M
            Assert.Equal(1_950_000m, PayrollCalculator.ProgressivePit(18_000_000m, brackets)); // + 8M × 15%
            Assert.Equal(4_750_000m, PayrollCalculator.ProgressivePit(32_000_000m, brackets)); // + 14M × 20%
            Assert.Equal(9_750_000m, PayrollCalculator.ProgressivePit(52_000_000m, brackets)); // + 20M × 25%
            Assert.Equal(18_150_000m, PayrollCalculator.ProgressivePit(80_000_000m, brackets)); // + 28M × 30%
            Assert.Equal(25_150_000m, PayrollCalculator.ProgressivePit(100_000_000m, brackets)); // + 20M × 35%
        }

        [Fact]
        public void ProgressivePit_Is_Progressive_Not_A_Cliff()
        {
            // Vượt 1 đồng qua ngưỡng 5 triệu chỉ tăng thêm 10 xu (phần vượt chịu thuế suất 10%), không nhảy cả khoản.
            var brackets = PayrollTestData.Brackets();
            Assert.Equal(250_000.10m, PayrollCalculator.ProgressivePit(5_000_001m, brackets));
        }

        [Fact]
        public void ProgressivePit_Equals_Closed_Form_Formula_From_Circular_111()
        {
            // Công thức rút gọn của từng bậc phải khớp với cộng dồn từng phần.
            var brackets = PayrollTestData.Brackets();
            Assert.Equal(10_000_000m * 0.10m - 250_000m, PayrollCalculator.ProgressivePit(10_000_000m, brackets));
            Assert.Equal(32_000_000m * 0.20m - 1_650_000m, PayrollCalculator.ProgressivePit(32_000_000m, brackets));
            Assert.Equal(80_000_000m * 0.30m - 5_850_000m, PayrollCalculator.ProgressivePit(80_000_000m, brackets));
        }

        // ===== BHXH có trần =====

        [Fact]
        public void Insurance_Is_Capped_At_20_Times_Statutory_Base()
        {
            // Lương cơ sở 2.340.000 → trần 46.800.000. Lương khai báo 50.000.000 bị cắt về trần.
            var rules = PayrollTestData.Rules(statutoryBase: 2_340_000m);
            var input = PayrollTestData.FullMonth(baseSalary: 50_000_000m, declared: 50_000_000m);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(46_800_000m, result.InsuranceCap);
            Assert.Equal(46_800_000m, result.InsuranceBase);
            // Người lao động: BHXH 8%, BHYT 1,5%, BHTN 1%
            Assert.Equal(3_744_000m, result.EmployeeSocial);
            Assert.Equal(702_000m, result.EmployeeHealth);
            Assert.Equal(468_000m, result.EmployeeUnemployment);
            Assert.Equal(4_914_000m, result.EmployeeTotal);
        }

        [Fact]
        public void Employer_Share_Is_Calculated_On_The_Capped_Base()
        {
            var rules = PayrollTestData.Rules(statutoryBase: 2_340_000m);
            var input = PayrollTestData.FullMonth(baseSalary: 50_000_000m, declared: 50_000_000m);

            var result = PayrollCalculator.Calculate(input, rules);

            // Trên căn cứ 46.800.000: hưu trí 14%, ốm đau thai sản 3%, BHYT 3%, BHTN 1%, TNLĐ-BNN 0,5%
            Assert.Equal(6_552_000m, result.EmployerPension);
            Assert.Equal(1_404_000m, result.EmployerSocial - result.EmployerPension);
            Assert.Equal(7_956_000m, result.EmployerSocial);
            Assert.Equal(1_404_000m, result.EmployerHealth);
            Assert.Equal(468_000m, result.EmployerUnemployment);
            Assert.Equal(234_000m, result.EmployerAccident);
            Assert.Equal(10_062_000m, result.EmployerTotal);
        }

        [Fact]
        public void Insurance_Below_Cap_Uses_Declared_Salary()
        {
            var rules = PayrollTestData.Rules(statutoryBase: 2_340_000m);
            var input = PayrollTestData.FullMonth(baseSalary: 10_000_000m, declared: 10_000_000m);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(10_000_000m, result.InsuranceBase);
            Assert.Equal(800_000m, result.EmployeeSocial);
            Assert.Equal(1_050_000m, result.EmployeeTotal);
        }

        [Fact]
        public void Insurance_Cap_Follows_The_Statutory_Base_Of_The_Period()
        {
            // Từ 01/07/2026 lương cơ sở 2.530.000 → trần 50.600.000, nên 50.000.000 không bị cắt.
            var rules = PayrollTestData.Rules(statutoryBase: 2_530_000m);
            var input = PayrollTestData.FullMonth(baseSalary: 50_000_000m, declared: 50_000_000m);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(50_600_000m, result.InsuranceCap);
            Assert.Equal(50_000_000m, result.InsuranceBase);
        }

        [Fact]
        public void Declared_Insurance_Salary_Defaults_To_Contract_Salary_When_Missing()
        {
            var rules = PayrollTestData.Rules(statutoryBase: 2_340_000m);
            var input = PayrollTestData.FullMonth(baseSalary: 12_000_000m, declared: null);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(12_000_000m, result.InsuranceBase);
        }

        // ===== Phạt đi muộn =====

        [Theory]
        [InlineData(0, 0)]
        [InlineData(14, 0)]
        [InlineData(15, 1)]
        [InlineData(29, 1)]
        [InlineData(30, 2)]
        [InlineData(61, 4)]
        public void LatePenalty_Charges_Each_Full_Block_Only(int minutes, int expectedBlocks)
        {
            Assert.Equal(expectedBlocks * 50_000m, PayrollCalculator.LatePenalty(minutes, 15, 50_000m));
        }

        [Fact]
        public void LatePenalty_Rejects_Non_Positive_Block_Size()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PayrollCalculator.LatePenalty(30, 0, 50_000m));
        }

        [Fact]
        public void Calculate_Deducts_Late_Penalty_From_Net()
        {
            var rules = PayrollTestData.Rules();
            var input = PayrollTestData.FullMonth(baseSalary: 10_000_000m, declared: 10_000_000m);
            var withoutLate = PayrollCalculator.Calculate(input, rules);

            var lateInput = PayrollTestData.FullMonth(baseSalary: 10_000_000m, declared: 10_000_000m, lateMinutes: 35);
            var withLate = PayrollCalculator.Calculate(lateInput, rules);

            Assert.Equal(100_000m, withLate.LatePenalty); // 35 phút = 2 block × 50.000
            Assert.Equal(withoutLate.NetSalary - 100_000m, withLate.NetSalary);
        }

        // ===== Tăng ca =====

        [Fact]
        public void Overtime_Uses_Hourly_Rate_From_Daily_Rate_At_150_Percent()
        {
            // Lương hợp đồng 22.000.000 / 22 ngày = 1.000.000/ngày; /8 giờ = 125.000/giờ; 2 giờ × 1,5
            var rules = PayrollTestData.Rules();
            var input = PayrollTestData.FullMonth(baseSalary: 22_000_000m, declared: 22_000_000m, overtimeHours: 2m);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(375_000m, result.OvertimeSalary);
        }

        [Fact]
        public void Overtime_Multiplier_Comes_From_Rules()
        {
            var rules = PayrollTestData.Rules(overtimeMultiplier: 2m);
            var input = PayrollTestData.FullMonth(baseSalary: 22_000_000m, declared: 22_000_000m, overtimeHours: 2m);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(500_000m, result.OvertimeSalary);
        }

        // ===== Nghỉ không lương và nghỉ có lương =====

        [Fact]
        public void Unpaid_Leave_Is_Deducted_When_There_Is_No_Attendance()
        {
            // Không có chấm công: coi như đi đủ 22 ngày, trừ 2 ngày nghỉ không lương.
            var rules = PayrollTestData.Rules();
            var input = PayrollTestData.Input(baseSalary: 22_000_000m, hasAttendance: false, unpaidLeave: 2m);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(20m, result.EligibleDays);
            Assert.Equal(20_000_000m, result.StandardWorkSalary);
        }

        [Fact]
        public void Unpaid_Leave_Is_Not_Deducted_Twice_When_Attendance_Exists()
        {
            // Đã có chấm công: ngày nghỉ không lương không có mặt trong ngày công thực tế, không trừ thêm lần nữa.
            var rules = PayrollTestData.Rules();
            var input = PayrollTestData.Input(baseSalary: 22_000_000m, hasAttendance: true, actualWorkDays: 21m, unpaidLeave: 1m);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(21m, result.EligibleDays);
            Assert.Equal(21_000_000m, result.StandardWorkSalary);
        }

        [Fact]
        public void Paid_Leave_Counts_As_Worked_Days_And_Is_Capped_At_Standard_Days()
        {
            var rules = PayrollTestData.Rules();
            var input = PayrollTestData.Input(baseSalary: 22_000_000m, hasAttendance: true, actualWorkDays: 20m, paidLeave: 2m);

            var result = PayrollCalculator.Calculate(input, rules);
            Assert.Equal(22m, result.EligibleDays);

            var overCap = PayrollTestData.Input(baseSalary: 22_000_000m, hasAttendance: true, actualWorkDays: 21m, paidLeave: 3m);
            Assert.Equal(22m, PayrollCalculator.Calculate(overCap, rules).EligibleDays);
        }

        [Fact]
        public void Unpaid_Leave_Larger_Than_Standard_Days_Does_Not_Go_Negative()
        {
            var rules = PayrollTestData.Rules();
            var input = PayrollTestData.Input(baseSalary: 22_000_000m, hasAttendance: false, unpaidLeave: 30m);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(0m, result.EligibleDays);
            Assert.Equal(0m, result.StandardWorkSalary);
        }

        // ===== Thuế TNCN tổng hợp: miễn thuế theo hạn mức, giảm trừ, người phụ thuộc =====

        [Fact]
        public void Full_Tax_Example_Uses_Exempt_Limits_And_Deductions()
        {
            // Lương 30.000.000; ăn trưa 730.000 (miễn, hạn mức 730.000); xăng xe 500.000 (chịu thuế);
            // BH người lao động 3.150.000; giảm trừ bản thân 15.500.000; 1 người phụ thuộc 6.200.000.
            // Gross = 31.230.000; không chịu thuế = 730.000 + 3.150.000 + 15.500.000 + 6.200.000 = 25.580.000
            // Thu nhập tính thuế = 5.650.000 → thuế = 5.000.000 × 5% + 650.000 × 10% = 315.000
            var rules = PayrollTestData.Rules(statutoryBase: 2_340_000m);
            var input = PayrollTestData.FullMonth(
                baseSalary: 30_000_000m,
                declared: 30_000_000m,
                allowances: new[]
                {
                    new AllowanceLine("LUNCH", 730_000m, true, 730_000m),
                    new AllowanceLine("FUEL_PHONE", 500_000m, false, null)
                },
                dependents: 1);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(31_230_000m, result.Gross);
            Assert.Equal(730_000m, result.TaxExemptAllowance);
            Assert.Equal(25_580_000m, result.NonTaxableIncome);
            Assert.Equal(5_650_000m, result.TaxableIncome);
            Assert.Equal(315_000m, result.PersonalIncomeTax);
            Assert.Equal(27_765_000m, result.NetSalary);
        }

        [Fact]
        public void Exempt_Allowance_Above_Limit_Is_Taxed_On_The_Excess()
        {
            var rules = PayrollTestData.Rules();
            var input = PayrollTestData.FullMonth(
                baseSalary: 10_000_000m,
                declared: 10_000_000m,
                allowances: new[] { new AllowanceLine("LUNCH", 1_000_000m, true, 730_000m) });

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(730_000m, result.TaxExemptAllowance);
        }

        [Fact]
        public void Exempt_Allowance_Without_Limit_Is_Fully_Exempt()
        {
            var rules = PayrollTestData.Rules();
            var input = PayrollTestData.FullMonth(
                baseSalary: 10_000_000m,
                declared: 10_000_000m,
                allowances: new[] { new AllowanceLine("MEAL_IN_KIND", 1_000_000m, true, null) });

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(1_000_000m, result.TaxExemptAllowance);
        }

        [Fact]
        public void Income_Below_Deductions_Pays_No_Tax()
        {
            // Lương 10.000.000; BH 1.050.000; giảm trừ 15.500.000 → thu nhập tính thuế bằng 0
            var rules = PayrollTestData.Rules();
            var input = PayrollTestData.FullMonth(baseSalary: 10_000_000m, declared: 10_000_000m);

            var result = PayrollCalculator.Calculate(input, rules);

            Assert.Equal(0m, result.TaxableIncome);
            Assert.Equal(0m, result.PersonalIncomeTax);
        }

        [Fact]
        public void Each_Dependent_Adds_The_Dependent_Deduction()
        {
            var rules = PayrollTestData.Rules();
            var none = PayrollCalculator.Calculate(
                PayrollTestData.FullMonth(baseSalary: 40_000_000m, declared: 40_000_000m, dependents: 0), rules);
            var two = PayrollCalculator.Calculate(
                PayrollTestData.FullMonth(baseSalary: 40_000_000m, declared: 40_000_000m, dependents: 2), rules);

            Assert.Equal(12_400_000m, two.DependentDeduction);
            Assert.Equal(none.TaxableIncome - 12_400_000m, two.TaxableIncome);
        }
    }

    /// <summary>Dữ liệu mẫu dùng chung cho các test lương.</summary>
    internal static class PayrollTestData
    {
        public static List<PitBracketOption> Brackets() => new()
        {
            new PitBracketOption { UpTo = 5_000_000m, Rate = 0.05m },
            new PitBracketOption { UpTo = 10_000_000m, Rate = 0.10m },
            new PitBracketOption { UpTo = 18_000_000m, Rate = 0.15m },
            new PitBracketOption { UpTo = 32_000_000m, Rate = 0.20m },
            new PitBracketOption { UpTo = 52_000_000m, Rate = 0.25m },
            new PitBracketOption { UpTo = 80_000_000m, Rate = 0.30m },
            new PitBracketOption { UpTo = null, Rate = 0.35m }
        };

        public static InsuranceRateRule Rates() => new()
        {
            EmployeeSocial = 0.08m,
            EmployeeHealth = 0.015m,
            EmployeeUnemployment = 0.01m,
            EmployerPension = 0.14m,
            EmployerSickMaternity = 0.03m,
            EmployerHealth = 0.03m,
            EmployerUnemployment = 0.01m,
            EmployerAccident = 0.005m
        };

        public static PayrollRules Rules(
            decimal statutoryBase = 2_340_000m,
            decimal overtimeMultiplier = 1.5m) => new()
        {
            StatutoryBaseSalary = statutoryBase,
            PersonalDeduction = 15_500_000m,
            DependentDeduction = 6_200_000m,
            Insurance = Rates(),
            PitBrackets = Brackets(),
            InsuranceCapMultiplier = 20m,
            LateBlockMinutes = 15,
            LateAmountPerBlock = 50_000m,
            OvertimeMultiplier = overtimeMultiplier,
            HoursPerDay = 8m
        };

        /// <summary>Nhân viên đi làm đủ 22 ngày công.</summary>
        public static PayrollInput FullMonth(
            decimal baseSalary,
            decimal? declared,
            decimal overtimeHours = 0m,
            int lateMinutes = 0,
            AllowanceLine[]? allowances = null,
            int dependents = 0) => new()
        {
            BaseSalary = baseSalary,
            DeclaredInsuranceSalary = declared,
            StandardDays = 22,
            HasAttendance = true,
            ActualWorkDays = 22m,
            OvertimeHours = overtimeHours,
            TotalLateMinutes = lateMinutes,
            DependentCount = dependents,
            Allowances = allowances ?? Array.Empty<AllowanceLine>()
        };

        public static PayrollInput Input(
            decimal baseSalary,
            bool hasAttendance,
            decimal actualWorkDays = 0m,
            decimal paidLeave = 0m,
            decimal unpaidLeave = 0m) => new()
        {
            BaseSalary = baseSalary,
            StandardDays = 22,
            HasAttendance = hasAttendance,
            ActualWorkDays = actualWorkDays,
            PaidLeaveDays = paidLeave,
            UnpaidLeaveDays = unpaidLeave
        };
    }
}

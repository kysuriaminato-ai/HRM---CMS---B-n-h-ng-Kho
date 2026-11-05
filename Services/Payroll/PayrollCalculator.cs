using System;
using System.Collections.Generic;
using System.Linq;

namespace ERP_HRM_CMS.Services.Payroll
{
    /// <summary>
    /// Một khoản phụ cấp của kỳ lương. Cờ miễn thuế và hạn mức lấy từ bảng PayrollAllowanceTypes.
    /// </summary>
    /// <param name="Code">Mã loại phụ cấp (ví dụ LUNCH).</param>
    /// <param name="Amount">Số tiền trong kỳ (đồng).</param>
    /// <param name="IsTaxExempt">Có được miễn thuế TNCN hay không.</param>
    /// <param name="TaxExemptLimit">Hạn mức miễn thuế mỗi tháng. Null = không giới hạn khi được miễn.</param>
    public sealed record AllowanceLine(string Code, decimal Amount, bool IsTaxExempt, decimal? TaxExemptLimit = null);

    /// <summary>Dữ liệu đầu vào của một nhân viên trong một kỳ lương.</summary>
    public sealed class PayrollInput
    {
        /// <summary>Lương hợp đồng (lương làm căn cứ tính lương ngày công).</summary>
        public decimal BaseSalary { get; init; }

        /// <summary>Lương đóng bảo hiểm khai báo trong hợp đồng. Null = lấy bằng lương hợp đồng.</summary>
        public decimal? DeclaredInsuranceSalary { get; init; }

        /// <summary>Số ngày công chuẩn của kỳ.</summary>
        public int StandardDays { get; init; } = 22;

        /// <summary>Có dữ liệu chấm công trong kỳ hay không. Không có thì coi như đi làm đủ, trừ nghỉ không lương.</summary>
        public bool HasAttendance { get; init; }

        /// <summary>Số ngày công thực tế (Present, Late, Early).</summary>
        public decimal ActualWorkDays { get; init; }

        /// <summary>Số ngày nghỉ có lương đã duyệt (phép năm, ốm, công tác).</summary>
        public decimal PaidLeaveDays { get; init; }

        /// <summary>Số ngày nghỉ không lương đã duyệt.</summary>
        public decimal UnpaidLeaveDays { get; init; }

        /// <summary>Tổng số phút đi muộn trong kỳ.</summary>
        public int TotalLateMinutes { get; init; }

        /// <summary>Tổng số giờ làm thêm trong kỳ.</summary>
        public decimal OvertimeHours { get; init; }

        /// <summary>Phụ cấp chức vụ (chịu thuế).</summary>
        public decimal PositionAllowance { get; init; }

        /// <summary>Thưởng cố định / thỏa thuận (chịu thuế).</summary>
        public decimal Bonus { get; init; }

        /// <summary>Thưởng hiệu suất KPI theo kỳ (từ KpiReviews đã duyệt).</summary>
        public decimal KpiBonus { get; set; } = 0;

        /// <summary>Khen thưởng từ Quyết định nhân sự (HrDecisions loại Reward).</summary>
        public decimal DecisionReward { get; set; } = 0;

        /// <summary>Khấu trừ kỷ luật từ Quyết định nhân sự (HrDecisions loại Discipline).</summary>
        public decimal DecisionDiscipline { get; set; } = 0;

        /// <summary>Số người phụ thuộc còn hiệu lực trong kỳ.</summary>
        public int DependentCount { get; init; }

        /// <summary>Các khoản phụ cấp theo bảng dữ liệu.</summary>
        public IReadOnlyList<AllowanceLine> Allowances { get; init; } = Array.Empty<AllowanceLine>();
    }

    /// <summary>Kết quả tính lương của một nhân viên. Mọi số tiền đã làm tròn đến đồng.</summary>
    public sealed class PayrollResult
    {
        // Ngày công và thu nhập
        public decimal EligibleDays { get; init; }
        public decimal StandardWorkSalary { get; init; }
        public decimal OvertimeSalary { get; init; }
        public decimal AllowanceTotal { get; init; }
        public decimal KpiBonus { get; init; }
        public decimal DecisionReward { get; init; }
        public decimal DecisionDiscipline { get; init; }
        public decimal TotalBonus { get; init; }
        public decimal Gross { get; init; }

        // Bảo hiểm
        public decimal InsuranceCap { get; init; }
        public decimal InsuranceBase { get; init; }
        public decimal EmployeeSocial { get; init; }
        public decimal EmployeeHealth { get; init; }
        public decimal EmployeeUnemployment { get; init; }
        public decimal EmployeeTotal { get; init; }
        public decimal EmployerPension { get; init; }
        public decimal EmployerSocial { get; init; }
        public decimal EmployerHealth { get; init; }
        public decimal EmployerUnemployment { get; init; }
        public decimal EmployerAccident { get; init; }
        public decimal EmployerTotal { get; init; }

        // Thuế TNCN
        public decimal TaxExemptAllowance { get; init; }
        public decimal PersonalDeduction { get; init; }
        public decimal DependentDeduction { get; init; }
        public decimal NonTaxableIncome { get; init; }
        public decimal TaxableIncome { get; init; }
        public decimal PersonalIncomeTax { get; init; }

        // Khấu trừ và thực lĩnh
        public decimal LatePenalty { get; init; }
        public decimal NetSalary { get; init; }
    }

    /// <summary>
    /// Công thức tính lương thuần, không truy cập database nên có thể kiểm thử đơn vị.
    /// </summary>
    public static class PayrollCalculator
    {
        /// <summary>Làm tròn đến đồng, nửa đồng thì làm tròn ra xa số 0.</summary>
        public static decimal Money(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);

        public static PayrollResult Calculate(PayrollInput input, PayrollRules rules)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(rules);

            // 1. Ngày công được tính lương
            decimal standardDays = input.StandardDays;
            decimal eligibleDays = input.HasAttendance
                // Ngày nghỉ không lương đã nằm ngoài ngày công thực tế, không trừ thêm lần nữa.
                ? Math.Min(input.ActualWorkDays + input.PaidLeaveDays, standardDays)
                // Không có chấm công: coi như đi làm đủ, chỉ trừ ngày nghỉ không lương.
                : Math.Max(0m, standardDays - input.UnpaidLeaveDays);

            decimal dailyRate = standardDays > 0 ? input.BaseSalary / standardDays : 0m;
            decimal hourlyRate = rules.HoursPerDay > 0 ? dailyRate / rules.HoursPerDay : 0m;

            decimal standardWorkSalary = Money(dailyRate * eligibleDays);
            decimal overtimeSalary = Money(hourlyRate * rules.OvertimeMultiplier * input.OvertimeHours);

            // 2. Phụ cấp: tổng tiền và phần được miễn thuế (theo cờ và hạn mức của từng loại)
            decimal allowanceTotal = input.Allowances.Sum(a => a.Amount);
            decimal taxExemptAllowance = input.Allowances
                .Where(a => a.IsTaxExempt)
                .Sum(a => a.TaxExemptLimit is decimal limit ? Math.Min(a.Amount, limit) : a.Amount);

            // 3. Tổng thưởng: Bonus + KpiBonus + DecisionReward
            decimal totalBonus = input.Bonus + input.KpiBonus + input.DecisionReward;

            decimal gross = standardWorkSalary + overtimeSalary + input.PositionAllowance + allowanceTotal + totalBonus;

            // 4. Bảo hiểm: lương đóng BH không vượt trần = hệ số × lương cơ sở của kỳ
            decimal insuranceCap = Money(rules.InsuranceCapMultiplier * rules.StatutoryBaseSalary);
            decimal declared = input.DeclaredInsuranceSalary ?? input.BaseSalary;
            decimal insuranceBase = Math.Max(0m, Math.Min(declared, insuranceCap));

            var r = rules.Insurance;
            decimal eeSocial = Money(insuranceBase * r.EmployeeSocial);
            decimal eeHealth = Money(insuranceBase * r.EmployeeHealth);
            decimal eeUnemployment = Money(insuranceBase * r.EmployeeUnemployment);
            decimal erPension = Money(insuranceBase * r.EmployerPension);
            decimal erSick = Money(insuranceBase * r.EmployerSickMaternity);
            decimal erHealth = Money(insuranceBase * r.EmployerHealth);
            decimal erUnemployment = Money(insuranceBase * r.EmployerUnemployment);
            decimal erAccident = Money(insuranceBase * r.EmployerAccident);

            decimal employeeTotal = eeSocial + eeHealth + eeUnemployment;
            decimal employerSocial = erPension + erSick;
            decimal employerTotal = employerSocial + erHealth + erUnemployment + erAccident;

            // 5. Phạt đi muộn
            decimal latePenalty = LatePenalty(input.TotalLateMinutes, rules.LateBlockMinutes, rules.LateAmountPerBlock);

            // 6. Thuế TNCN: trừ phần miễn thuế, bảo hiểm người lao động đóng, giảm trừ bản thân và người phụ thuộc
            decimal personalDeduction = rules.PersonalDeduction;
            decimal dependentDeduction = rules.DependentDeduction * input.DependentCount;
            decimal nonTaxable = taxExemptAllowance + employeeTotal + personalDeduction + dependentDeduction;
            decimal taxableIncome = Money(Math.Max(0m, gross - nonTaxable));
            decimal pit = Money(ProgressivePit(taxableIncome, rules.PitBrackets));

            // 7. Thực lĩnh: Gross trừ các khoản bảo hiểm NLĐ, thuế TNCN, phạt đi muộn và trừ kỷ luật từ QĐ
            decimal totalDeductions = employeeTotal + pit + latePenalty + input.DecisionDiscipline;
            decimal net = Money(Math.Max(0m, gross - totalDeductions));

            return new PayrollResult
            {
                EligibleDays = eligibleDays,
                StandardWorkSalary = standardWorkSalary,
                OvertimeSalary = overtimeSalary,
                AllowanceTotal = allowanceTotal,
                KpiBonus = input.KpiBonus,
                DecisionReward = input.DecisionReward,
                DecisionDiscipline = input.DecisionDiscipline,
                TotalBonus = totalBonus,
                Gross = Money(gross),
                InsuranceCap = insuranceCap,
                InsuranceBase = insuranceBase,
                EmployeeSocial = eeSocial,
                EmployeeHealth = eeHealth,
                EmployeeUnemployment = eeUnemployment,
                EmployeeTotal = employeeTotal,
                EmployerPension = erPension,
                EmployerSocial = employerSocial,
                EmployerHealth = erHealth,
                EmployerUnemployment = erUnemployment,
                EmployerAccident = erAccident,
                EmployerTotal = employerTotal,
                TaxExemptAllowance = Money(taxExemptAllowance),
                PersonalDeduction = personalDeduction,
                DependentDeduction = dependentDeduction,
                NonTaxableIncome = Money(nonTaxable),
                TaxableIncome = taxableIncome,
                PersonalIncomeTax = pit,
                LatePenalty = latePenalty,
                NetSalary = net
            };
        }

        /// <summary>
        /// Thuế lũy tiến từng phần: mỗi phần thu nhập chỉ chịu thuế suất của bậc chứa nó.
        /// Các bậc phải được sắp xếp tăng dần; bậc cuối có UpTo = null.
        /// </summary>
        public static decimal ProgressivePit(decimal taxableIncome, IReadOnlyList<PitBracketOption> brackets)
        {
            ArgumentNullException.ThrowIfNull(brackets);
            if (brackets.Count == 0)
                throw new ArgumentException("Thiếu biểu thuế lũy tiến.", nameof(brackets));

            if (taxableIncome <= 0m) return 0m;

            decimal tax = 0m;
            decimal lower = 0m;
            foreach (var bracket in brackets)
            {
                decimal upper = bracket.UpTo ?? decimal.MaxValue;
                if (taxableIncome <= lower) break;

                decimal portion = Math.Min(taxableIncome, upper) - lower;
                tax += portion * bracket.Rate;
                lower = upper;
            }

            return tax;
        }

        /// <summary>Phạt đi muộn: mỗi block đủ số phút trễ bị phạt một lần, phần lẻ không bị phạt.</summary>
        public static decimal LatePenalty(int totalLateMinutes, int blockMinutes, decimal amountPerBlock)
        {
            if (blockMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockMinutes), "Số phút của mỗi block phải lớn hơn 0.");

            if (totalLateMinutes <= 0) return 0m;

            int blocks = totalLateMinutes / blockMinutes;
            return blocks * amountPerBlock;
        }
    }
}

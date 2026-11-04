/*
  Thêm 10 cột còn thiếu trong database cũ: PayrollPeriods.TotalEmployerInsurance và 9 cột của SalarySlips
  (DependentCount, DependentDeduction, PersonalDeduction, TaxExemptAllowance, TotalEmployerInsurance,
  EmployerSocialInsurance, EmployerHealthInsurance, EmployerUnemploymentInsurance, EmployerAccidentInsurance).
  - Chỉ THÊM cột. Không sửa, không xóa dữ liệu hiện có.
  - Dòng cũ nhận giá trị 0 (kỳ lương/phiếu lương cũ không có các khoản này). Mặc định tạm được xóa sau khi thêm,
    nên cấu trúc khớp model EF.
  - Idempotent: cột đã có thì bỏ qua. Chạy trong transaction.
  - Sao lưu database trước khi chạy.
*/
USE [ERP_HRM_CMS_DB];
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- PayrollPeriods.TotalEmployerInsurance
IF COL_LENGTH(N'dbo.PayrollPeriods', N'TotalEmployerInsurance') IS NULL
BEGIN
    ALTER TABLE dbo.PayrollPeriods ADD TotalEmployerInsurance DECIMAL(18,2) NOT NULL CONSTRAINT DF_tmp_PayrollPeriods_TotalEmployerInsurance DEFAULT (0);
    ALTER TABLE dbo.PayrollPeriods DROP CONSTRAINT DF_tmp_PayrollPeriods_TotalEmployerInsurance;
    PRINT N'Đã thêm cột dbo.PayrollPeriods.TotalEmployerInsurance.';
END;

-- SalarySlips.DependentCount
IF COL_LENGTH(N'dbo.SalarySlips', N'DependentCount') IS NULL
BEGIN
    ALTER TABLE dbo.SalarySlips ADD DependentCount INT NOT NULL CONSTRAINT DF_tmp_SalarySlips_DependentCount DEFAULT (0);
    ALTER TABLE dbo.SalarySlips DROP CONSTRAINT DF_tmp_SalarySlips_DependentCount;
    PRINT N'Đã thêm cột dbo.SalarySlips.DependentCount.';
END;

-- SalarySlips.DependentDeduction
IF COL_LENGTH(N'dbo.SalarySlips', N'DependentDeduction') IS NULL
BEGIN
    ALTER TABLE dbo.SalarySlips ADD DependentDeduction DECIMAL(18,2) NOT NULL CONSTRAINT DF_tmp_SalarySlips_DependentDeduction DEFAULT (0);
    ALTER TABLE dbo.SalarySlips DROP CONSTRAINT DF_tmp_SalarySlips_DependentDeduction;
    PRINT N'Đã thêm cột dbo.SalarySlips.DependentDeduction.';
END;

-- SalarySlips.EmployerAccidentInsurance
IF COL_LENGTH(N'dbo.SalarySlips', N'EmployerAccidentInsurance') IS NULL
BEGIN
    ALTER TABLE dbo.SalarySlips ADD EmployerAccidentInsurance DECIMAL(18,2) NOT NULL CONSTRAINT DF_tmp_SalarySlips_EmployerAccidentInsurance DEFAULT (0);
    ALTER TABLE dbo.SalarySlips DROP CONSTRAINT DF_tmp_SalarySlips_EmployerAccidentInsurance;
    PRINT N'Đã thêm cột dbo.SalarySlips.EmployerAccidentInsurance.';
END;

-- SalarySlips.EmployerHealthInsurance
IF COL_LENGTH(N'dbo.SalarySlips', N'EmployerHealthInsurance') IS NULL
BEGIN
    ALTER TABLE dbo.SalarySlips ADD EmployerHealthInsurance DECIMAL(18,2) NOT NULL CONSTRAINT DF_tmp_SalarySlips_EmployerHealthInsurance DEFAULT (0);
    ALTER TABLE dbo.SalarySlips DROP CONSTRAINT DF_tmp_SalarySlips_EmployerHealthInsurance;
    PRINT N'Đã thêm cột dbo.SalarySlips.EmployerHealthInsurance.';
END;

-- SalarySlips.EmployerSocialInsurance
IF COL_LENGTH(N'dbo.SalarySlips', N'EmployerSocialInsurance') IS NULL
BEGIN
    ALTER TABLE dbo.SalarySlips ADD EmployerSocialInsurance DECIMAL(18,2) NOT NULL CONSTRAINT DF_tmp_SalarySlips_EmployerSocialInsurance DEFAULT (0);
    ALTER TABLE dbo.SalarySlips DROP CONSTRAINT DF_tmp_SalarySlips_EmployerSocialInsurance;
    PRINT N'Đã thêm cột dbo.SalarySlips.EmployerSocialInsurance.';
END;

-- SalarySlips.EmployerUnemploymentInsurance
IF COL_LENGTH(N'dbo.SalarySlips', N'EmployerUnemploymentInsurance') IS NULL
BEGIN
    ALTER TABLE dbo.SalarySlips ADD EmployerUnemploymentInsurance DECIMAL(18,2) NOT NULL CONSTRAINT DF_tmp_SalarySlips_EmployerUnemploymentInsurance DEFAULT (0);
    ALTER TABLE dbo.SalarySlips DROP CONSTRAINT DF_tmp_SalarySlips_EmployerUnemploymentInsurance;
    PRINT N'Đã thêm cột dbo.SalarySlips.EmployerUnemploymentInsurance.';
END;

-- SalarySlips.PersonalDeduction
IF COL_LENGTH(N'dbo.SalarySlips', N'PersonalDeduction') IS NULL
BEGIN
    ALTER TABLE dbo.SalarySlips ADD PersonalDeduction DECIMAL(18,2) NOT NULL CONSTRAINT DF_tmp_SalarySlips_PersonalDeduction DEFAULT (0);
    ALTER TABLE dbo.SalarySlips DROP CONSTRAINT DF_tmp_SalarySlips_PersonalDeduction;
    PRINT N'Đã thêm cột dbo.SalarySlips.PersonalDeduction.';
END;

-- SalarySlips.TaxExemptAllowance
IF COL_LENGTH(N'dbo.SalarySlips', N'TaxExemptAllowance') IS NULL
BEGIN
    ALTER TABLE dbo.SalarySlips ADD TaxExemptAllowance DECIMAL(18,2) NOT NULL CONSTRAINT DF_tmp_SalarySlips_TaxExemptAllowance DEFAULT (0);
    ALTER TABLE dbo.SalarySlips DROP CONSTRAINT DF_tmp_SalarySlips_TaxExemptAllowance;
    PRINT N'Đã thêm cột dbo.SalarySlips.TaxExemptAllowance.';
END;

-- SalarySlips.TotalEmployerInsurance
IF COL_LENGTH(N'dbo.SalarySlips', N'TotalEmployerInsurance') IS NULL
BEGIN
    ALTER TABLE dbo.SalarySlips ADD TotalEmployerInsurance DECIMAL(18,2) NOT NULL CONSTRAINT DF_tmp_SalarySlips_TotalEmployerInsurance DEFAULT (0);
    ALTER TABLE dbo.SalarySlips DROP CONSTRAINT DF_tmp_SalarySlips_TotalEmployerInsurance;
    PRINT N'Đã thêm cột dbo.SalarySlips.TotalEmployerInsurance.';
END;

COMMIT TRANSACTION;
PRINT N'Hoàn tất kiểm tra các cột còn thiếu.';

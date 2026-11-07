using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_HRM_CMS.Migrations
{
    /// <inheritdoc />
    public partial class PhaseC1_AspNetCoreIdentity_And_CustomerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceAdjustmentRequests_Users_ApprovedById",
                table: "AttendanceAdjustmentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceLocks_Users_LockedById",
                table: "AttendanceLocks");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceLocks_Users_UnlockedById",
                table: "AttendanceLocks");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Users_UserId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Contents_Users_AuthorId",
                table: "Contents");

            migrationBuilder.DropForeignKey(
                name: "FK_DebtPayments_Users_CreatedBy",
                table: "DebtPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAssetAssignments_Users_AssignedById",
                table: "EmployeeAssetAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAssetAssignments_Users_ReturnedById",
                table: "EmployeeAssetAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeChangeHistories_Users_ChangedBy",
                table: "EmployeeChangeHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_Users_UploadedBy",
                table: "EmployeeDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_HrDecisions_Users_ApprovedById",
                table: "HrDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSchedules_Users_InterviewerUserId",
                table: "InterviewSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Users_CreatedBy",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_CreatedBy",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiReviews_Users_ReviewerId",
                table: "KpiReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_CreatedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_DepartmentHeadId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_DirectorId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Users_UploadedBy",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_OnboardingChecklists_Users_AssignedHrId",
                table: "OnboardingChecklists");

            migrationBuilder.DropForeignKey(
                name: "FK_OnboardingTasks_Users_AssignedToUserId",
                table: "OnboardingTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_CreatedBy",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_CreatedBy",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_Users_CreatedBy",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_ApprovedBy",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_CreatedBy",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_StocktakingSheets_Users_CreatedBy",
                table: "StocktakingSheets");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissions_Users_UserId",
                table: "UserPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");

            // Tạo bảng AspNetUsers mới (không xóa bảng Users trong Phiên C1)
            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    SecurityStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Copy toàn bộ dữ liệu từ bảng Users sang AspNetUsers (giữ nguyên Id, PasswordHash, PasswordSalt, SecurityStamp...)
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [AspNetUsers] ON;
INSERT INTO [AspNetUsers] (
    [Id], [EmployeeId], [Username], [NormalizedUserName],
    [Email], [NormalizedEmail], [EmailConfirmed],
    [PasswordHash], [PasswordSalt], [IsActive], [IsSuperAdmin], [MustChangePassword],
    [SecurityStamp], [ConcurrencyStamp],
    [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled],
    [LockoutEnd], [LockoutEnabled], [AccessFailedCount],
    [LastLoginAt], [CreatedAt]
)
SELECT
    u.[Id], u.[EmployeeId], u.[Username], UPPER(u.[Username]),
    NULL, NULL, 0,
    u.[PasswordHash], u.[PasswordSalt], u.[IsActive], u.[IsSuperAdmin], u.[MustChangePassword],
    u.[SecurityStamp], CAST(NEWID() AS nvarchar(36)),
    NULL, 0, 0,
    NULL, 1, 0,
    u.[LastLoginAt], u.[CreatedAt]
FROM [Users] u;
SET IDENTITY_INSERT [AspNetUsers] OFF;
");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Username",
                table: "AspNetUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmployeeId",
                table: "AspNetUsers",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceAdjustmentRequests_AspNetUsers_ApprovedById",
                table: "AttendanceAdjustmentRequests",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLocks_AspNetUsers_LockedById",
                table: "AttendanceLocks",
                column: "LockedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLocks_AspNetUsers_UnlockedById",
                table: "AttendanceLocks",
                column: "UnlockedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_AspNetUsers_UserId",
                table: "CartItems",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contents_AspNetUsers_AuthorId",
                table: "Contents",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DebtPayments_AspNetUsers_CreatedBy",
                table: "DebtPayments",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAssetAssignments_AspNetUsers_AssignedById",
                table: "EmployeeAssetAssignments",
                column: "AssignedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAssetAssignments_AspNetUsers_ReturnedById",
                table: "EmployeeAssetAssignments",
                column: "ReturnedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeChangeHistories_AspNetUsers_ChangedBy",
                table: "EmployeeChangeHistories",
                column: "ChangedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocuments_AspNetUsers_UploadedBy",
                table: "EmployeeDocuments",
                column: "UploadedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HrDecisions_AspNetUsers_ApprovedById",
                table: "HrDecisions",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSchedules_AspNetUsers_InterviewerUserId",
                table: "InterviewSchedules",
                column: "InterviewerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_AspNetUsers_CreatedBy",
                table: "InventoryTransactions",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_AspNetUsers_CreatedBy",
                table: "Invoices",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiReviews_AspNetUsers_ReviewerId",
                table: "KpiReviews",
                column: "ReviewerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_CreatedByUserId",
                table: "LeaveRequests",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_DepartmentHeadId",
                table: "LeaveRequests",
                column: "DepartmentHeadId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_DirectorId",
                table: "LeaveRequests",
                column: "DirectorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_AspNetUsers_UploadedBy",
                table: "Media",
                column: "UploadedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingChecklists_AspNetUsers_AssignedHrId",
                table: "OnboardingChecklists",
                column: "AssignedHrId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingTasks_AspNetUsers_AssignedToUserId",
                table: "OnboardingTasks",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_CreatedBy",
                table: "Orders",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_CreatedBy",
                table: "Payments",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_CreatedBy",
                table: "PayrollPeriods",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ApprovedBy",
                table: "PurchaseOrders",
                column: "ApprovedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_CreatedBy",
                table: "PurchaseOrders",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StocktakingSheets_AspNetUsers_CreatedBy",
                table: "StocktakingSheets",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissions_AspNetUsers_UserId",
                table: "UserPermissions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_AspNetUsers_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Employees_EmployeeId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceAdjustmentRequests_AspNetUsers_ApprovedById",
                table: "AttendanceAdjustmentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceLocks_AspNetUsers_LockedById",
                table: "AttendanceLocks");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceLocks_AspNetUsers_UnlockedById",
                table: "AttendanceLocks");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_AspNetUsers_UserId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Contents_AspNetUsers_AuthorId",
                table: "Contents");

            migrationBuilder.DropForeignKey(
                name: "FK_DebtPayments_AspNetUsers_CreatedBy",
                table: "DebtPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAssetAssignments_AspNetUsers_AssignedById",
                table: "EmployeeAssetAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAssetAssignments_AspNetUsers_ReturnedById",
                table: "EmployeeAssetAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeChangeHistories_AspNetUsers_ChangedBy",
                table: "EmployeeChangeHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_AspNetUsers_UploadedBy",
                table: "EmployeeDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_HrDecisions_AspNetUsers_ApprovedById",
                table: "HrDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSchedules_AspNetUsers_InterviewerUserId",
                table: "InterviewSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_AspNetUsers_CreatedBy",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_AspNetUsers_CreatedBy",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiReviews_AspNetUsers_ReviewerId",
                table: "KpiReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_CreatedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_DepartmentHeadId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_DirectorId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_AspNetUsers_UploadedBy",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_OnboardingChecklists_AspNetUsers_AssignedHrId",
                table: "OnboardingChecklists");

            migrationBuilder.DropForeignKey(
                name: "FK_OnboardingTasks_AspNetUsers_AssignedToUserId",
                table: "OnboardingTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_CreatedBy",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_CreatedBy",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_CreatedBy",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ApprovedBy",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_CreatedBy",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_StocktakingSheets_AspNetUsers_CreatedBy",
                table: "StocktakingSheets");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissions_AspNetUsers_UserId",
                table: "UserPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_AspNetUsers_UserId",
                table: "UserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            // Đồng bộ các bản ghi mới phát sinh trên AspNetUsers về lại bảng Users trước khi xóa AspNetUsers khi rollback Down
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] (
    [Id], [EmployeeId], [Username], [PasswordHash], [PasswordSalt],
    [IsActive], [IsSuperAdmin], [MustChangePassword], [SecurityStamp],
    [LastLoginAt], [CreatedAt]
)
SELECT
    a.[Id], a.[EmployeeId], a.[Username], LEFT(a.[PasswordHash], 256), a.[PasswordSalt],
    a.[IsActive], a.[IsSuperAdmin], a.[MustChangePassword], a.[SecurityStamp],
    a.[LastLoginAt], a.[CreatedAt]
FROM [AspNetUsers] a
WHERE NOT EXISTS (SELECT 1 FROM [Users] u WHERE u.[Id] = a.[Id]);
SET IDENTITY_INSERT [Users] OFF;
");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceAdjustmentRequests_Users_ApprovedById",
                table: "AttendanceAdjustmentRequests",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLocks_Users_LockedById",
                table: "AttendanceLocks",
                column: "LockedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLocks_Users_UnlockedById",
                table: "AttendanceLocks",
                column: "UnlockedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Users_UserId",
                table: "CartItems",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contents_Users_AuthorId",
                table: "Contents",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DebtPayments_Users_CreatedBy",
                table: "DebtPayments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAssetAssignments_Users_AssignedById",
                table: "EmployeeAssetAssignments",
                column: "AssignedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAssetAssignments_Users_ReturnedById",
                table: "EmployeeAssetAssignments",
                column: "ReturnedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeChangeHistories_Users_ChangedBy",
                table: "EmployeeChangeHistories",
                column: "ChangedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocuments_Users_UploadedBy",
                table: "EmployeeDocuments",
                column: "UploadedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HrDecisions_Users_ApprovedById",
                table: "HrDecisions",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSchedules_Users_InterviewerUserId",
                table: "InterviewSchedules",
                column: "InterviewerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Users_CreatedBy",
                table: "InventoryTransactions",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CreatedBy",
                table: "Invoices",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiReviews_Users_ReviewerId",
                table: "KpiReviews",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_CreatedByUserId",
                table: "LeaveRequests",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_DepartmentHeadId",
                table: "LeaveRequests",
                column: "DepartmentHeadId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_DirectorId",
                table: "LeaveRequests",
                column: "DirectorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Users_UploadedBy",
                table: "Media",
                column: "UploadedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingChecklists_Users_AssignedHrId",
                table: "OnboardingChecklists",
                column: "AssignedHrId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingTasks_Users_AssignedToUserId",
                table: "OnboardingTasks",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_CreatedBy",
                table: "Orders",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_CreatedBy",
                table: "Payments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_Users_CreatedBy",
                table: "PayrollPeriods",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_ApprovedBy",
                table: "PurchaseOrders",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_CreatedBy",
                table: "PurchaseOrders",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StocktakingSheets_Users_CreatedBy",
                table: "StocktakingSheets",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissions_Users_UserId",
                table: "UserPermissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

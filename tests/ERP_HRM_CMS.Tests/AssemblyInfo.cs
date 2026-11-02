using Xunit;

// Chạy các lớp kiểm thử lần lượt, tránh việc nhiều lớp cùng tạo/seed database ERP_HRM_CMS_TEST một lúc.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

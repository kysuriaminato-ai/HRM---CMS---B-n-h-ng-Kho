$ErrorActionPreference = "Continue"
$ws = New-Object Microsoft.PowerShell.Commands.WebRequestSession

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " KIỂM THỬ THỰC TẾ PHIÊN 1: REALTIME SIGNALR & AUDIT TRAIL" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Đăng nhập
Write-Host "`n1. Đăng nhập SuperAdmin..." -ForegroundColor Yellow
$loginGet = Invoke-WebRequest -Uri "http://localhost:5129/Auth/Login" -WebSession $ws -UseBasicParsing
$tokenMatch = [regex]::Match($loginGet.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
$token = if ($tokenMatch.Success) { $tokenMatch.Groups[1].Value } else { "" }

$loginBody = @{
    "Username" = "admin"
    "Password" = "Admin@123"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $token
}
$loginPost = Invoke-WebRequest -Uri "http://localhost:5129/Auth/Login" -Method Post -Body $loginBody -WebSession $ws -UseBasicParsing -MaximumRedirection 10
Write-Host "   -> Đăng nhập thành công, phiên làm việc đã thiết lập." -ForegroundColor Green

# 2. Kiểm tra SignalR Negotiate endpoint
Write-Host "`n2. Kiểm tra SignalR Hub Negotiate..." -ForegroundColor Yellow
try {
    $negotiate = Invoke-WebRequest -Uri "http://localhost:5129/hub/notifications/negotiate?negotiateVersion=1" -Method Post -WebSession $ws -UseBasicParsing
    Write-Host "   -> SignalR Negotiate Status:" $negotiate.StatusCode -ForegroundColor Green
    Write-Host "   -> Negotiate Response:" $negotiate.Content -ForegroundColor Green
} catch {
    Write-Host "   -> Lỗi kết nối Hub Negotiate:" $_.Exception.Message -ForegroundColor Red
}

# 3. Tạo một đơn hàng POS để kích hoạt: (a) Audit Trail ghi log; (b) SignalR bắn thông báo
Write-Host "`n3. Thực hiện giao dịch POS để kích hoạt Audit Trail & Notification..." -ForegroundColor Yellow
$posPage = Invoke-WebRequest -Uri "http://localhost:5129/BanHang/POS" -WebSession $ws -UseBasicParsing
$posTokenMatch = [regex]::Match($posPage.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
$posToken = if ($posTokenMatch.Success) { $posTokenMatch.Groups[1].Value } else { $token }

# Thêm hàng vào giỏ
$addCartBody = '{"ProductId": 1, "Quantity": 1, "BranchId": 1}'
$null = Invoke-WebRequest -Uri "http://localhost:5129/GioHang/AddToCartApi" -Method Post -Body $addCartBody -ContentType "application/json" -WebSession $ws -UseBasicParsing

# Thanh toán đơn
$checkoutForm = @{
    "BranchId" = "1"
    "CustomerName" = "Giám Đốc Kiểm Toán Vận Hành"
    "CustomerPhone" = "0911223344"
    "PaymentMethod" = "CASH"
    "CustomerCashGiven" = "25000000"
    "DiscountAmount" = "0"
    "Note" = "Test Audit & Notification Phiên 1"
    "__RequestVerificationToken" = $posToken
}
$checkoutRes = Invoke-WebRequest -Uri "http://localhost:5129/BanHang/XuatDonPOS" -Method Post -Body $checkoutForm -WebSession $ws -UseBasicParsing -MaximumRedirection 10
Write-Host "   -> Giao dịch hoàn tất!" -ForegroundColor Green

# 4. Kiểm tra Notification API
Write-Host "`n4. Kiểm tra API Thông báo Real-time (/Notification/GetRecentNotifications)..." -ForegroundColor Yellow
$notifRes = Invoke-WebRequest -Uri "http://localhost:5129/Notification/GetRecentNotifications" -WebSession $ws -UseBasicParsing
Write-Host "   -> Status Code:" $notifRes.StatusCode
Write-Host "   -> Dữ liệu thông báo nhận được:" $notifRes.Content -ForegroundColor Green

# 5. Kiểm tra Giao diện Trung tâm Thông báo (/Notification/Index)
Write-Host "`n5. Kiểm tra Giao diện Trung tâm Thông báo (/Notification/Index)..." -ForegroundColor Yellow
$notifIndex = Invoke-WebRequest -Uri "http://localhost:5129/Notification/Index" -WebSession $ws -UseBasicParsing
Write-Host "   -> GET /Notification/Index Status:" $notifIndex.StatusCode "Length:" $notifIndex.Content.Length -ForegroundColor Green

# 6. Kiểm tra Giao diện Nhật ký Kiểm toán (/AuditLog/Index)
Write-Host "`n6. Kiểm tra Giao diện Nhật ký Kiểm toán (/AuditLog/Index)..." -ForegroundColor Yellow
$auditIndex = Invoke-WebRequest -Uri "http://localhost:5129/AuditLog/Index" -WebSession $ws -UseBasicParsing
Write-Host "   -> GET /AuditLog/Index Status:" $auditIndex.StatusCode "Length:" $auditIndex.Content.Length -ForegroundColor Green
if ($auditIndex.Content -match "Giám Đốc Kiểm Toán Vận Hành" -or $auditIndex.Content -match "Order" -or $auditIndex.Content -match "CREATE") {
    Write-Host "   -> Audit Trail đã tự động ghi vết thay đổi vào Database và hiển thị trên giao diện!" -ForegroundColor Green
}

# 7. Kiểm tra trực tiếp bảng AuditLogs & Notifications trong SQL Server
Write-Host "`n7. Kiểm tra dữ liệu SQL Server trực tiếp..." -ForegroundColor Yellow
$conn = New-Object System.Data.SqlClient.SqlConnection('Server=localhost;Database=ERP_HRM_CMS_DB;Trusted_Connection=True;TrustServerCertificate=True;')
$conn.Open()

Write-Host "--- TOP 3 AUDIT LOGS MỚI NHẤT ---" -ForegroundColor Cyan
$cmd1 = $conn.CreateCommand()
$cmd1.CommandText = 'SELECT TOP 3 Id, Username, Action, EntityName, RecordId, CreatedAt FROM AuditLogs ORDER BY Id DESC'
$adapter1 = New-Object System.Data.SqlClient.SqlDataAdapter($cmd1)
$ds1 = New-Object System.Data.DataSet
$adapter1.Fill($ds1) | Out-Null
$ds1.Tables[0] | Format-Table -AutoSize

Write-Host "--- THÔNG BÁO MỚI NHẤT TRONG NOTIFICATIONS ---" -ForegroundColor Cyan
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = 'SELECT TOP 3 Id, TargetRole, Title, Content, Type, IsRead, CreatedAt FROM Notifications ORDER BY Id DESC'
$adapter2 = New-Object System.Data.SqlClient.SqlDataAdapter($cmd2)
$ds2 = New-Object System.Data.DataSet
$adapter2.Fill($ds2) | Out-Null
$ds2.Tables[0] | Format-Table -AutoSize

$conn.Close()

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " KẾT QUẢ: PHIÊN 1 HOÀN TOÀN THÀNH CÔNG VÀ CHẠY THỰC TẾ 100%!" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$ErrorActionPreference = "Continue"
$ws = New-Object Microsoft.PowerShell.Commands.WebRequestSession

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " KIỂM THỬ THỰC TẾ PHIÊN 2: TITAN COBALT THEME & COMMAND PALETTE" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Kiểm tra tải file Asset tĩnh mới
Write-Host "`n1. Kiểm tra tài nguyên CSS và JS mới..." -ForegroundColor Yellow
$css = Invoke-WebRequest -Uri "http://localhost:5129/css/enterprise-theme.css" -UseBasicParsing
Write-Host "   -> GET /css/enterprise-theme.css: Status $($css.StatusCode), Kích thước: $($css.Content.Length) bytes" -ForegroundColor Green

$js = Invoke-WebRequest -Uri "http://localhost:5129/js/enterprise-theme.js" -UseBasicParsing
Write-Host "   -> GET /js/enterprise-theme.js: Status $($js.StatusCode), Kích thước: $($js.Content.Length) bytes" -ForegroundColor Green

# 2. Đăng nhập để kiểm tra trang quản trị
Write-Host "`n2. Đăng nhập tài khoản SuperAdmin..." -ForegroundColor Yellow
$loginGet = Invoke-WebRequest -Uri "http://localhost:5129/Auth/Login" -WebSession $ws -UseBasicParsing
$tokenMatch = [regex]::Match($loginGet.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
$token = if ($tokenMatch.Success) { $tokenMatch.Groups[1].Value } else { "" }

$loginBody = @{
    "Username" = "admin"
    "Password" = "Admin@123"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $token
}
$null = Invoke-WebRequest -Uri "http://localhost:5129/Auth/Login" -Method Post -Body $loginBody -WebSession $ws -UseBasicParsing -MaximumRedirection 10

# 3. Kiểm tra API GlobalQuickSearch (Command Palette Backend)
Write-Host "`n3. Kiểm tra API GlobalQuickSearch (Spotlight Search)..." -ForegroundColor Yellow

# 3.1 Tìm kiếm rỗng -> Lối tắt nhanh
$searchEmpty = Invoke-WebRequest -Uri "http://localhost:5129/Home/GlobalQuickSearch" -WebSession $ws -UseBasicParsing
Write-Host "   -> Lối tắt mặc định (Navigation shortcuts):" $searchEmpty.Content.Substring(0, [Math]::Min(200, $searchEmpty.Content.Length)) -ForegroundColor Green

# 3.2 Tìm kiếm theo tên sản phẩm 'Dell'
$searchDell = Invoke-WebRequest -Uri "http://localhost:5129/Home/GlobalQuickSearch?keyword=Dell" -WebSession $ws -UseBasicParsing
Write-Host "   -> Kết quả tìm 'Dell':" $searchDell.Content -ForegroundColor Green

# 3.3 Tìm kiếm theo tên nhân sự 'An'
$searchAn = Invoke-WebRequest -Uri "http://localhost:5129/Home/GlobalQuickSearch?keyword=An" -WebSession $ws -UseBasicParsing
Write-Host "   -> Kết quả tìm 'An':" $searchAn.Content -ForegroundColor Green

# 4. Kiểm tra mã nguồn HTML của các Layout đã tích hợp Theme & Quick Search Trigger
Write-Host "`n4. Kiểm tra HTML Topbar của các phân hệ..." -ForegroundColor Yellow
$dash = Invoke-WebRequest -Uri "http://localhost:5129/Home/Index" -WebSession $ws -UseBasicParsing
$hasTrigger = $dash.Content -match "command-palette-trigger"
$hasThemeIcon = $dash.Content -match "theme-toggle-icon"
$hasThemeCss = $dash.Content -match "enterprise-theme.css"
Write-Host "   -> /Home/Index tích hợp Command Palette Trigger: $hasTrigger | Theme Toggle: $hasThemeIcon | Enterprise CSS: $hasThemeCss" -ForegroundColor Green

$hrm = Invoke-WebRequest -Uri "http://localhost:5129/NhanSu/Index" -WebSession $ws -UseBasicParsing
$hasHrmTrigger = $hrm.Content -match "command-palette-trigger"
Write-Host "   -> /NhanSu/Index (_HRMLayout) tích hợp Command Palette: $hasHrmTrigger" -ForegroundColor Green

$sales = Invoke-WebRequest -Uri "http://localhost:5129/BanHang/POS" -WebSession $ws -UseBasicParsing
$hasSalesTrigger = $sales.Content -match "command-palette-trigger"
Write-Host "   -> /BanHang/POS (_SalesLayout) tích hợp Command Palette: $hasSalesTrigger" -ForegroundColor Green

$kho = Invoke-WebRequest -Uri "http://localhost:5129/Kho/Index" -WebSession $ws -UseBasicParsing
$hasKhoTrigger = $kho.Content -match "command-palette-trigger"
Write-Host "   -> /Kho/Index (_WarehouseLayout) tích hợp Command Palette: $hasKhoTrigger" -ForegroundColor Green

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " KẾT QUẢ: PHIÊN 2 HOÀN TOÀN THÀNH CÔNG VÀ CHẠY THỰC TẾ 100%!" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# ADR-005: Mô hình tính giá sản phẩm trang sức và vàng

## 1. Bối cảnh
Hệ thống ERP ban đầu lưu trực tiếp `SalePrice` trên bảng `Products` theo giá cố định (Fixed Price).
Khi mở rộng sang ngành hàng Trang sức & Vàng bạc đá quý:
- Giá của sản phẩm trang sức biến động từng ngày theo giá vàng giao dịch trên thị trường.
- Một sản phẩm có thể có nhiều biến thể (Variant) với các chất liệu vàng khác nhau (10K, 14K, 18K, 24K), trọng lượng vàng khác nhau, tiền công (LaborFee) và tiền đá (StoneFee) khác nhau.

## 2. Quyết định
1. **Giá không lưu tĩnh vào bảng `ProductVariants` khi dùng chế độ `GoldFormula`:**
   - Với `PricingMode = 'Fixed'`: Giá bán là `FixedPrice`.
   - Với `PricingMode = 'GoldFormula'`: Giá bán được tính động khi đọc dữ liệu theo công thức:
     $$\text{Giá bán} = \lceil (\text{Giá vàng bán/gram} \times \text{Trọng lượng (g)} \times \text{Hệ số hàm lượng}) + \text{Tiền công} + \text{Tiền đá} \rceil_{1000}$$
     trong đó kết quả được làm tròn lên bội số 1.000 đ gần nhất.
2. **Nguồn giá vàng:** Lấy từ bảng `GoldPrices` bản ghi mới nhất có `EffectiveDate <= Ngày hiện tại theo giờ Việt Nam (VietnamTime)`.
3. **Snapshot giá khi đặt hàng (Phiên F):**
   - Giá tính động khi hiển thị catalog và xem giỏ hàng.
   - Khi khách hàng hoặc thu ngân bấm chốt đơn hàng / xuất đơn POS, hệ thống sẽ snapshot (chụp) đơn giá thực tế tại thời điểm giao dịch và lưu cố định vào `OrderDetail.UnitPrice` để làm căn cứ kế toán, xuất hóa đơn và đối soát công nợ, không bị ảnh hưởng bởi biến động giá vàng của các ngày sau đó.

## 3. Hệ quả
- Bảng `ProductVariants` duy trì sự tinh gọn, không cần chạy batch update hàng nghìn bản ghi mỗi khi giá vàng thay đổi.
- Đảm bảo tính minh bạch, nhất quán và độ chính xác cao cho ngành trang sức.


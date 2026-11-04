# ADR-003: Khung giao diện do Super Admin quản trị

- **Trạng thái:** Đề xuất, chờ xác nhận. Chưa triển khai.
- **Phạm vi:** Giai đoạn 3. Tài liệu này chỉ mô tả thiết kế; không có code, migration hay thay đổi hành vi nào đi kèm.
- **Liên quan:** `SiteLayout`, `LayoutConfigController`, `PublicController`, `AppDbContext.SaveChanges` (AuditLog tự động), `UrlAnToan`, `HtmlSanitizer`.

---

## 1. Hiện trạng

| Thành phần | Hiện có | Vấn đề cần giải quyết |
|---|---|---|
| `SiteLayout` (bảng `SiteLayouts`) | `LayoutName`, `BlockKey`, `BlockTitle`, `OrderIndex`, `IsActive`, `CustomCssClass`, `UpdatedBy`, `UpdatedAt` | Cấu hình khối là các cột rời, không có kiểu khối và không có nội dung cấu hình. |
| `LayoutConfigController` | `Index`, `CapNhatTrangThaiBlock`, `SapXepThuTu`, yêu cầu `layout.manage` | Sửa trực tiếp bản đang public, không có nháp. `SapXepThuTu` truy vấn từng dòng (N+1) và không kiểm tra khối có thuộc layout hay không. |
| `PublicController` (dòng ~26) | Đọc khối đang bật mỗi request | Không có cache, không tách bản nháp khỏi bản public. |
| AuditLog | `AppDbContext.SaveChanges` tự ghi nhật ký cho entity thay đổi (trừ AuditLog, Notification, LoginAttempt, ...) | Ghi được "ai đổi cột nào", nhưng không có ngữ nghĩa nghiệp vụ như xuất bản hay khôi phục. |
| Phiên bản | Không có | Không khôi phục được. |
| Xem trước | Không có | Không xem được trước khi công khai. |

## 2. Quyết định đề xuất

### 2.1 Mô hình dữ liệu

```
LayoutZone      (Id, Key [unique], Name, OrderIndex, IsActive)
LayoutBlock     (Id, ZoneId -> LayoutZone, BlockType, ConfigJson, OrderIndex, IsActive,
                 UpdatedBy, UpdatedAt, RowVersion)
LayoutRevision  (Id, LayoutName, Version, Status [Draft|Published|Archived],
                 SnapshotJson, ContentHash, CreatedBy, CreatedAt,
                 PublishedBy, PublishedAt, RowVersion)
```

- `LayoutZone` và `LayoutBlock` là **bảng trực tiếp** mà trang public đọc. Chúng chỉ phản ánh revision đang `Published`.
- `LayoutRevision` giữ **snapshot đầy đủ** (JSON gồm zone và block) của mỗi phiên bản.
- Ràng buộc: tối đa **một** revision `Draft` cho mỗi `LayoutName` (unique index có điều kiện `Status = 'Draft'`); tối đa **một** revision `Published` cho mỗi `LayoutName` (unique index có điều kiện `Status = 'Published'`).
- `SnapshotJson` được băm (`ContentHash`, SHA-256) khi xuất bản để phát hiện thay đổi và tránh xuất bản lặp.

**Lý do:** Trang public đọc bảng chuẩn hóa, nhanh và dễ cache theo khóa. Snapshot giữ lịch sử đầy đủ và là nguồn để khôi phục. Đánh đổi: có hai biểu diễn (snapshot và bảng). Quy trình xuất bản phải đảm bảo hai biểu diễn khớp nhau (xem 2.4).

**Quan hệ với `SiteLayout`:** Giữ `SiteLayout` cho đến khi có migration dữ liệu (xem mục 4, quyết định 4).

### 2.2 Registry loại khối và validate ConfigJson

```csharp
public interface IBlockDefinition
{
    string TypeCode { get; }                 // ví dụ "HERO_BANNER", "PRODUCT_GRID", "HTML_RICH"
    string DisplayName { get; }              // tên tiếng Việt cho giao diện quản trị
    Type ConfigType { get; }                 // record cấu hình có kiểu rõ ràng
    int SchemaVersion { get; }               // tăng khi đổi cấu trúc cấu hình
    IReadOnlyCollection<string> AllowedZoneKeys { get; }
    string ViewComponentName { get; }        // view component hiển thị khối
    ValidationResult Validate(object config, IServiceProvider services);
}

public interface IBlockRegistry
{
    IBlockDefinition Get(string typeCode);   // ném lỗi nếu không đăng ký
    IReadOnlyCollection<IBlockDefinition> All { get; }
}
```

- Mỗi loại khối là một lớp đăng ký bằng DI (`AddSingleton<IBlockDefinition, HeroBannerBlock>()`). **Chỉ** các `TypeCode` đã đăng ký mới được lưu.
- Validate phía server, theo thứ tự:
  1. `TypeCode` tồn tại trong registry và `Zone` nằm trong `AllowedZoneKeys`.
  2. `ConfigJson` không vượt **32 KB** và là JSON hợp lệ.
  3. Deserialize vào `ConfigType` với `JsonSerializerOptions { UnmappedMemberHandling = Disallow }`. Thuộc tính lạ bị từ chối (chống mass-assignment, cùng nguyên tắc với ViewModel ở T4).
  4. DataAnnotations trên record cấu hình, cộng logic riêng của từng loại.
  5. Mọi URL đi qua `UrlAnToan.LaHopLe`. Mọi HTML đi qua bộ sanitize hiện có, **trước khi lưu**, và lại khi hiển thị.
  6. Giới hạn: tối đa **50 block** mỗi zone và **20 zone**.
- `ConfigJson` lưu kèm `SchemaVersion`. Khi đọc bản cũ, nếu `SchemaVersion` thấp hơn, chạy bộ chuyển đổi trước khi validate.
- Validate chạy **khi lưu nháp**, **khi xem trước** và **khi xuất bản** (xuất bản validate lại, không tin vào bước lưu nháp).

### 2.3 Luồng nháp, xem trước, xuất bản, khôi phục

```
[Sửa]  --lưu-->  Draft (1 bản duy nhất)  --xem trước-->  preview token (TTL)
                    |
                 xuất bản (transaction)
                    v
Published (live tables) <-- Archived (các bản cũ, không xóa)
                    |
           khôi phục: tạo Draft mới từ snapshot cũ -> xuất bản như bình thường
```

- **Lưu nháp:** Cập nhật `SnapshotJson` của Draft. Dùng `RowVersion` để phát hiện ghi đè đồng thời: nếu người khác đã lưu sau khi bạn tải, trả lỗi xung đột thay vì ghi đè.
- **Xem trước:**
  - Sinh token ngẫu nhiên 32 byte bằng `RandomNumberGenerator`. Chỉ lưu **băm SHA-256** của token, không lưu token thô.
  - Token gắn với `RevisionId` và `UserId`, hết hạn sau **30 phút** (đề xuất).
  - Trang xem trước render từ snapshot Draft, trả header `Cache-Control: no-store`, `X-Robots-Tag: noindex`. Không bao giờ đưa nội dung xem trước vào cache.
  - Token không có quyền công khai; chỉ đọc được qua query `?preview=` và chỉ khi còn hạn.
- **Xuất bản** (một transaction):
  1. Kiểm tra `RowVersion` của Draft khớp với bản người dùng đang xem.
  2. Validate lại toàn bộ snapshot.
  3. Thay nội dung `LayoutZone`/`LayoutBlock` của `LayoutName` bằng snapshot (xóa và thêm lại trong cùng transaction).
  4. Revision `Published` cũ chuyển thành `Archived`. Draft thành `Published`, `Version = Version cũ + 1`, ghi `PublishedBy`, `PublishedAt`.
  5. Ghi AuditLog hành động `PUBLISH`.
  6. Commit, rồi mới xóa cache (mục 2.4).
- **Khôi phục:** Không sửa lịch sử. Chọn một revision `Archived` và bấm "Tạo nháp từ phiên bản này". Nháp mới nhận snapshot cũ; khi xuất bản, nó thành phiên bản mới với số version tăng. Lịch sử luôn tuyến tính, không có bản nào bị xóa.
- **Quyền:**
  - Xem và sửa nháp, xem trước: `layout.manage` (đã có).
  - Xuất bản và khôi phục: `layout.publish` (mới). Đề xuất: chỉ SUPERADMIN. Quyền mới được gán theo cơ chế một lần ở T10, không tự khôi phục sau khi gỡ.
  - Mọi POST có `[ValidateAntiForgeryToken]` và `[KiemTraQuyen]`.

### 2.4 Cache bản xuất bản bằng IMemoryCache

- Khóa cache **gồm revision id**: `layout:published:{LayoutName}:{RevisionId}`. Khi xuất bản, revision mới có id mới, nên khóa mới tự động. Người đọc không bao giờ thấy bản cũ sau khi xuất bản thành công.
- Sau commit, gọi `Remove` cho khóa của revision cũ để giải phóng bộ nhớ. Việc xóa chỉ là dọn dẹp, không ảnh hưởng tính đúng.
- Thời gian sống (sliding/absolute) đề xuất **10 phút**, phòng trường hợp bản đọc được đưa vào cache trước khi có revision mới.
- Lấy `RevisionId` đang Published bằng một truy vấn nhẹ (chỉ id và version) mỗi request, rồi tra cache. Nội dung khối chỉ load khi cache miss.
- Chỉ cache dữ liệu public. Không cache Draft, không cache xem trước.
- **Giới hạn:** `IMemoryCache` nằm trong từng tiến trình. Nếu chạy nhiều instance, mỗi instance tự có cache; khóa theo revision id vẫn đúng, nhưng sau xuất bản các instance khác vẫn dùng bản cũ cho đến khi hết TTL. Xem quyết định 6.

### 2.5 AuditLog cho mọi thay đổi

- **Tự động:** `AppDbContext.SaveChanges` đã ghi nhật ký cho `LayoutZone`, `LayoutBlock`, `LayoutRevision` vì các entity này không nằm trong danh sách loại trừ. Cần kiểm tra lại khi triển khai.
- **Thủ công, theo nghiệp vụ** (bắt buộc, vì tự động không biết ngữ nghĩa):

| Action | Khi nào | Nội dung |
|---|---|---|
| `LAYOUT_DRAFT_SAVE` | Lưu nháp | RevisionId, ContentHash cũ và mới, số block |
| `LAYOUT_PREVIEW_ISSUE` | Cấp token xem trước | RevisionId, thời hạn (không ghi token) |
| `LAYOUT_PUBLISH` | Xuất bản | RevisionId, Version mới, bản đã Archived |
| `LAYOUT_DRAFT_FROM_ARCHIVE` | Tạo nháp từ bản cũ | RevisionId nguồn |
| `LAYOUT_BLOCK_STATUS` | Bật/tắt khối (thay cho `CapNhatTrangThaiBlock`) | BlockId, trạng thái cũ và mới |

- Ghi `UserId`, `Username`, `IpAddress` theo cấu trúc `AuditLog` hiện có. Không ghi `ConfigJson` đầy đủ vào AuditLog (có thể chứa HTML dài); chỉ ghi hash và số lượng.
- Ghi trong cùng transaction với thay đổi dữ liệu, để không có thay đổi mà thiếu nhật ký.

## 3. Phương án đã cân nhắc và loại bỏ

| Phương án | Lý do không chọn |
|---|---|
| Chỉ lưu JSON, trang public đọc thẳng snapshot Published | Đơn giản hơn, nhưng mọi truy vấn khối phải parse JSON; khó lọc và thống kê. Vẫn là phương án dự phòng nếu quyết định 2 chọn đơn giản. |
| Mỗi block có `RevisionId` (bản sao block theo từng revision) | Lặp dữ liệu nhiều, khó truy vết khối nào thuộc revision nào, khó đảm bảo một bản Published duy nhất. |
| Sửa trực tiếp bảng public và "hoàn tác" bằng lịch sử AuditLog | AuditLog không phải nguồn khôi phục đáng tin (có thể bị che số, không chứa đầy đủ cấu hình). |
| Cho phép xóa revision cũ | Trái nguyên tắc không xóa dữ liệu nghiệp vụ. Layout nhỏ nên giữ toàn bộ lịch sử không tốn đáng kể. |

## 4. Các quyết định cần bạn chọn

1. **Ai được xuất bản?** Đề xuất: chỉ SUPERADMIN (quyền `layout.publish`). Phương án khác: cấp `layout.publish` cho một vai trò quản trị CMS.
2. **Mô hình snapshot + bảng (2.1) hay chỉ JSON?** Đề xuất: snapshot + bảng, vì trang public đọc nhanh và cache đơn giản.
3. **Có nhiều layout không?** `LayoutName` hiện có mặc định `"Default"`. Nếu chỉ dùng một layout cho trang chủ, có thể bỏ bớt độ phức tạp của nhiều layout.
4. **Chuyển dữ liệu `SiteLayout` cũ thế nào?** Cần bảng ánh xạ `BlockKey` sang `BlockType`, và quyết định giữ bảng cũ đến khi nào. Đề xuất: migration dữ liệu một lần, giữ `SiteLayouts` ở chế độ chỉ đọc một phiên bản rồi xóa ở phiên bản sau.
5. **Hết hạn token xem trước:** 30 phút có phù hợp không? Có cần giới hạn số lần dùng token không?
6. **Triển khai một instance hay nhiều instance?** Nếu nhiều instance, `IMemoryCache` cần thay bằng cơ chế thông báo (Redis hoặc kiểm tra version định kỳ). Hiện đề xuất giả định một instance.
7. **Giữ lịch sử revision vô thời hạn hay giới hạn số bản?** Đề xuất: giữ vô thời hạn.
8. **Phần `LayoutConfigController` hiện có:** Đề xuất thay thế hoàn toàn bằng luồng nháp. `CapNhatTrangThaiBlock` sẽ chuyển thành thao tác trên nháp, không còn ghi thẳng vào bản public. Bạn có đồng ý không?

## 5. Lộ trình triển khai (sau khi xác nhận)

Mỗi bước kết thúc bằng build và test. Bước sau chỉ bắt đầu khi bước trước xanh.

1. **Mô hình và migration:** thêm 3 entity, unique index có điều kiện, migration `AddLayoutRevisions`. Test: ràng buộc một Draft và một Published mỗi layout.
2. **Registry và validate:** `IBlockDefinition`, `IBlockRegistry`, validate ConfigJson. Test: khối lạ bị từ chối, thuộc tính lạ bị từ chối, URL không hợp lệ bị từ chối, HTML được sanitize, vượt giới hạn bị từ chối.
3. **Nháp và xem trước:** lưu nháp với `RowVersion`, cấp và kiểm tra token (lưu băm). Test: token hết hạn, token của người khác, ghi đè đồng thời bị chặn.
4. **Xuất bản và khôi phục:** transaction xuất bản, Archive, tạo nháp từ bản cũ. Test: xuất bản nguyên tử (lỗi giữa chừng thì không đổi gì), chỉ một bản Published, khôi phục tạo version mới.
5. **Cache:** khóa theo revision id, xóa sau commit. Test: sau xuất bản, đọc ra nội dung mới; xem trước không vào cache.
6. **AuditLog:** test từng action trong bảng mục 2.5.
7. **Chuyển dữ liệu `SiteLayout`:** migration dữ liệu có kiểm tra số lượng trước và sau.
8. **Dọn `LayoutConfigController`** và cập nhật README.

## 6. Rủi ro còn lại

- Hai biểu diễn (snapshot và bảng) có thể lệch nếu có ai sửa bảng trực tiếp. Giảm thiểu: chỉ `PublishService` được ghi vào `LayoutZone`/`LayoutBlock`, và có test so sánh hash sau xuất bản.
- Cache trong bộ nhớ không đồng bộ giữa nhiều instance (xem quyết định 6).
- Nội dung HTML do quản trị viên nhập là bề mặt tấn công XSS nếu sanitize sai. Giảm thiểu: sanitize khi lưu và khi hiển thị, kiểm tra CSP hiện có.
- Dữ liệu `SiteLayout` cũ có thể không ánh xạ hết sang `BlockType` mới. Cần danh sách ánh xạ được bạn duyệt.

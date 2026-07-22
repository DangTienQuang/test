# Backend note: Hoàn thiện luồng Overload Suggestion cho Customer Mobile

**Gửi:** SmartWash Backend Team  
**Ngày review:** 2026-07-23  
**Backend repository:** `SmartWash-BE`  
**Branch:** `feature/smart-booking-suggestions-and-vip-lane-operations`  
**Commit đã review:** `f17922f`  
**Kết luận:** Chưa thể nghiệm thu end-to-end. Các phần xử lý bên trong đã được cải thiện, nhưng API contract và luồng tạo suggestion thực tế vẫn còn lỗi P0.

---

## 1. Contract cuối cùng cần chốt

Frontend chỉ gửi quyết định của khách hàng. Frontend không gửi `suggestionId`, `suggestedBranchId`, `suggestedSlotId`, `suggestedTime`, voucher hoặc giá trị giảm.

### Endpoint

```http
POST /api/v1/bookings/{bookingId}/handle-overload-suggestion
Authorization: Bearer {customerAccessToken}
Content-Type: application/json
```

### Request body duy nhất

```json
{
  "decision": "Switch"
}
```

Giá trị hợp lệ:

- `Switch`: khách đồng ý chuyển sang chi nhánh được server đề xuất.
- `Cancel`: khách hủy booking vì chi nhánh quá tải.
- `Keep`: khách giữ nguyên booking và chấp nhận chờ.

Không chấp nhận các field sau từ frontend:

```json
{
  "suggestionId": 45,
  "suggestedBranchId": 2,
  "suggestedSlotId": 10,
  "suggestedTime": "2026-07-23T08:00:00Z",
  "alternativeBranchId": 2,
  "voucherCode": "...",
  "discountAmount": 50000
}
```

Backend phải lấy suggestion dựa trên:

1. `bookingId` trên URL.
2. `userId` lấy từ access token.
3. Suggestion chưa xử lý.
4. Suggestion chưa hết hạn.
5. Booking vẫn `Pending` và lịch hẹn chưa qua.

`suggestionId` vẫn có thể được trả trong GET/FCM để frontend theo dõi và debug, nhưng không phải dữ liệu đầu vào của API Handle.

---

## 2. Những phần commit hiện tại đã sửa đúng

Các phần sau có thể giữ lại:

- Quan hệ `Booking -> OverloadSuggestions` đã chuyển sang one-to-many.
- Unique index chỉ theo `BookingId` đã được bỏ ở migration Up.
- API GET đã lọc:
  - Suggestion chưa xử lý.
  - `ExpiresAt > UtcNow`.
  - Booking có status `Pending`.
  - `ScheduledTime > UtcNow`.
- GET đã trả thêm `suggestionId` và `expiresAt`.
- Transaction của API Handle bắt đầu trước khi đọc booking và suggestion.
- Switch lấy branch, slot và thời gian từ entity `OverloadSuggestion` trong DB.
- Switch kiểm tra lại destination capacity trước khi đổi booking.
- Switch đã trả voucher trong response.
- Cancel đã trả thông tin refund, điểm hoàn và voucher được khôi phục.
- Các conflict như suggestion hết hạn, đã xử lý hoặc slot đầy đã chuyển sang HTTP `409`.
- Migration FCM đã xóa token trùng trước khi tạo unique index.
- Backend build thành công với `0 errors`; lần kiểm tra gần nhất có `119 warnings`.

---

## 3. P0.1 — API Handle vẫn bắt frontend gửi `suggestionId`

### Hiện trạng

File:

```text
BLL/DTOs/HandleOverloadDecisionDTO.cs
```

DTO hiện tại:

```csharp
public class HandleOverloadDecisionDTO
{
    public int SuggestionId { get; set; }
    public string Decision { get; set; } = null!;
}
```

Service hiện tìm suggestion bằng:

```csharp
s.Id == request.SuggestionId && s.BookingId == bookingId
```

Nếu frontend gửi đúng contract:

```json
{
  "decision": "Switch"
}
```

ASP.NET sẽ nhận `SuggestionId = 0`. Service không tìm thấy row và trả:

```text
404 Overload suggestion not found.
```

### Cách sửa bắt buộc

DTO đề xuất:

```csharp
using System.ComponentModel.DataAnnotations;

public class HandleOverloadDecisionDTO
{
    [Required]
    public string Decision { get; set; } = null!;
}
```

Trong service, sau khi bắt đầu transaction và xác minh booking thuộc user, tìm suggestion từ DB:

```csharp
var now = DateTime.UtcNow;

var suggestion = await _context.OverloadSuggestions
    .Where(s => s.BookingId == bookingId
             && !s.IsProcessed
             && s.ExpiresAt > now)
    .OrderByDescending(s => s.CreatedAt)
    .FirstOrDefaultAsync();
```

Nên normalize decision một lần:

```csharp
var decision = request.Decision?.Trim();

if (!string.Equals(decision, "Switch", StringComparison.OrdinalIgnoreCase)
    && !string.Equals(decision, "Cancel", StringComparison.OrdinalIgnoreCase)
    && !string.Equals(decision, "Keep", StringComparison.OrdinalIgnoreCase))
{
    throw new BadRequestException("Decision must be Switch, Cancel, or Keep.");
}
```

Response nên trả decision ở format thống nhất `Switch`, `Cancel` hoặc `Keep`.

### Tiêu chí nghiệm thu

- Body chỉ có `decision` xử lý thành công.
- Body có `suggestionId` không được dùng để chọn suggestion.
- Không có active suggestion: trả `404` hoặc `409` theo contract đã thống nhất.
- Suggestion hết hạn: trả `409`.
- Suggestion đã xử lý: trả `409`.
- Booking không thuộc customer: trả `404`.

---

## 4. P0.2 — Endpoint relocation cũ tạo đường bypass

### Hiện trạng

Endpoint vẫn còn:

```http
POST /api/v1/bookings/{bookingId}/accept-relocation
```

Request cũ nhận:

```json
{
  "alternativeBranchId": 2,
  "voucherCode": "SURGE_REL_..."
}
```

Code nằm tại:

```text
API/Controllers/BookingsController.cs
BLL/Services/BookingService.cs - AcceptRelocationAsync
BLL/DTOs/BookingDTOs.cs - AcceptRelocationRequestDTO
```

Luồng này cho client chọn chi nhánh và voucher rồi trực tiếp sửa booking. Điều này đi ngược nguyên tắc server phải lấy thông tin relocation từ suggestion trong DB.

### Rủi ro

- Client có thể tự thay `alternativeBranchId`.
- Client có thể thử voucher code khác.
- Có hai endpoint cùng xử lý một nghiệp vụ nhưng rule khác nhau.
- Frontend hoặc tester có thể tích hợp nhầm endpoint cũ.
- Luồng cũ không có cùng transaction/concurrency protection với API Handle mới.

### Cách sửa khuyến nghị

Chọn một trong hai phương án:

1. Xóa/disable endpoint `accept-relocation` và DTO cũ; đây là phương án khuyến nghị.
2. Nếu phải giữ tương thích, endpoint cũ không được sử dụng dữ liệu branch/voucher từ body. Nó phải gọi chung logic Handle với decision `Switch` và tự lấy active suggestion trong DB.

Không duy trì hai implementation riêng cho cùng nghiệp vụ.

### Tiêu chí nghiệm thu

- Không còn endpoint nào cho customer tự gửi branch/slot/time/voucher để chuyển booking.
- Toàn bộ relocation đi qua một service/transaction duy nhất.

---

## 5. P0.3 — API manager “scan-and-notify” chưa tạo suggestion cho flow mới

### Hiện trạng

Endpoint:

```http
POST /api/v1/manager/branch-overload/scan-and-notify-relocation
```

`ManagerService.ScanAndNotifyRelocationAsync` hiện chỉ:

- Tìm pending booking.
- Chọn một alternative branch.
- Tạo voucher `SURGE_REL_*` kiểu cũ.
- Trả `RelocationProposalDTO`.

Method không:

- Tạo `OverloadSuggestion`.
- Gọi `IOverloadSuggestionService.CheckAndTriggerOverloadAsync`.
- Gửi FCM của flow mới.

Do đó quy trình test sau đang thất bại:

1. Manager gọi scan-and-notify.
2. Customer gọi GET overload suggestion.
3. API GET trả `data: null` vì manager endpoint không tạo row `OverloadSuggestions`.

### Cách sửa khuyến nghị

Manager endpoint phải gọi đúng flow mới:

```csharp
await _overloadSuggestionService.CheckAndTriggerOverloadAsync(branchId);
```

Tốt hơn, service trigger nên trả kết quả có cấu trúc:

```csharp
public class OverloadScanResultDTO
{
    public int ScannedBookings { get; set; }
    public int CreatedSuggestions { get; set; }
    public int SkippedActiveSuggestions { get; set; }
    public int NotificationsSent { get; set; }
    public int NotificationsFailed { get; set; }
}
```

Không tạo voucher compensation trước khi khách chọn Switch. Voucher Switch chỉ được tạo khi decision được commit thành công.

### Tiêu chí nghiệm thu

- Manager scan tạo row `OverloadSuggestion` cho booking đủ điều kiện.
- Customer GET thấy chính suggestion vừa tạo.
- FCM có `bookingId`, `suggestionId`, `expiresAt`.
- Scan lại trong TTL không tạo suggestion/notification trùng.
- Voucher chỉ xuất hiện sau khi customer Switch thành công.

---

## 6. P0.4 — Fire-and-forget đang dùng scoped DbContext

### Hiện trạng

Trong `OperationStaffService.CheckInBookingAsync`:

```csharp
_overloadSuggestionService.CheckAndTriggerOverloadAsync(booking.BranchId);
```

Method không được `await`. `OverloadSuggestionService` dùng `AutoWashDbContext` scoped theo HTTP request.

### Rủi ro

- Request kết thúc trước background task.
- DI scope bị dispose khi task vẫn đang query/save DB.
- Có thể phát sinh `ObjectDisposedException`.
- Exception không được await nên không trả về caller và khó quan sát log.
- Cùng một DbContext có thể bị sử dụng đồng thời, trong khi EF DbContext không thread-safe.

### Cách sửa

Phương án tối thiểu:

```csharp
await _overloadSuggestionService.CheckAndTriggerOverloadAsync(booking.BranchId);
```

Phương án production:

- Đẩy job vào background queue/Hangfire/Quartz/outbox.
- Worker tạo `IServiceScope` mới.
- Resolve `IOverloadSuggestionService` và DbContext mới trong scope của worker.
- Có retry, logging và idempotency key.

Không dùng `_ = Task.Run(...)` với service/DbContext scoped của request.

### Tiêu chí nghiệm thu

- Không còn pragma bỏ warning CS4014 cho overload trigger.
- Trigger failure được log đầy đủ.
- Không có unobserved exception hoặc disposed DbContext.

---

## 7. P0.5 — Check active suggestion rồi insert chưa atomic

### Hiện trạng

`OverloadSuggestionService` thực hiện:

1. Query active suggestion.
2. Nếu không có thì tìm branch/slot.
3. Insert suggestion mới.

Các bước này không nằm trong transaction có lock. Hai request trigger đồng thời có thể cùng thấy “không có active suggestion” và cùng insert.

Sau migration one-to-many, database không còn unique constraint ngăn trường hợp này.

### Hậu quả

- Một booking có hai active suggestion.
- Customer nhận hai push notification.
- Khi API Handle chỉ dựa trên bookingId, server không biết suggestion nào là duy nhất nếu không có rule rõ ràng.
- Capacity và thông tin branch đề xuất có thể khác nhau giữa hai row.

### Cách sửa khuyến nghị

Bao thao tác claim/create trong transaction và lock booking tương ứng. Có thể dùng `SELECT ... FOR UPDATE` cho row booking trước khi kiểm tra active suggestion.

Quy trình:

1. Begin transaction.
2. Lock booking row.
3. Kiểm tra active suggestion chưa hết hạn.
4. Nếu đã có, commit/return skipped.
5. Mark suggestion cũ hết hạn là processed nếu cần.
6. Insert đúng một suggestion mới.
7. Commit.
8. Gửi FCM sau commit hoặc qua outbox.

Nên có database guard cho active suggestion. Với MySQL có thể dùng nullable active key/generated column để unique index chỉ áp dụng cho row active.

### Tiêu chí nghiệm thu

- Gọi trigger song song 5–10 lần chỉ tạo đúng một active suggestion cho một booking.
- Chỉ gửi một notification trong TTL.
- Sau khi suggestion hết hạn/processed, hệ thống có thể tạo suggestion lịch sử mới.

---

## 8. P1 — Quy tắc transaction của API Handle

Việc bắt đầu Serializable transaction trước khi đọc booking/suggestion là đúng hướng. Tuy nhiên cần bảo đảm row suggestion được claim một cách rõ ràng.

### Quy trình đề xuất

```text
Begin transaction
  -> Load và lock booking theo bookingId + userId
  -> Kiểm tra booking Pending và ScheduledTime còn ở tương lai
  -> Load và lock active suggestion của booking
  -> Kiểm tra IsProcessed/ExpiresAt
  -> Validate decision
  -> Thực hiện Keep/Switch/Cancel
  -> Set suggestion.IsProcessed = true
  -> SaveChanges
Commit
```

Nếu hai request Handle đến cùng lúc:

- Chỉ một request được commit.
- Request còn lại trả `409 Conflict`.
- Không được trả `500` do deadlock/lock timeout không được map.

Backend cần map lỗi concurrency/deadlock của provider DB sang response `409` hoặc retry transaction có giới hạn.

---

## 9. P1 — Chi tiết xử lý từng decision

### Keep

- Booking giữ nguyên branch/time/capacity.
- Set `booking.IsWaitAccepted = true`.
- Set suggestion processed.
- Không tạo voucher.
- Không hoàn tiền.

### Switch

- Lấy destination branch/slot/time từ suggestion trong DB.
- Kiểm tra branch còn active.
- Kiểm tra DailySlotCapacity tồn tại và còn đủ tải.
- Trừ capacity slot cũ.
- Cộng capacity slot mới.
- Đổi branch và scheduled time của booking.
- Set suggestion processed.
- Commit transaction.
- Phát hành voucher bồi thường cho lần sử dụng sau theo policy hiện tại.
- Response trả `updatedBooking` và `voucher`.

Nếu slot mới đã đầy:

```http
409 Conflict
```

Toàn bộ thay đổi slot cũ phải rollback; không được làm mất capacity của booking.

### Cancel

- Trừ capacity slot hiện tại.
- Set booking `Cancelled`.
- Set suggestion processed.
- Hoàn tiền đúng một lần.
- Hoàn điểm đã dùng.
- Khôi phục voucher đã dùng.
- Response trả chi tiết refund.

Nếu payment PayOS được hoàn vào SmartWash Wallet thì phải ghi rõ trong API docs và response:

```json
{
  "refundDestination": "Wallet"
}
```

Refund transaction nên có `ReferenceBookingId` và có cơ chế kiểm tra refund đã tồn tại để bảo vệ idempotency.

---

## 10. P1 — Response contract đề xuất

### GET suggestion

```http
GET /api/v1/bookings/{bookingId}/overload-suggestion
```

```json
{
  "statusCode": 200,
  "message": "Success",
  "data": {
    "suggestionId": 45,
    "bookingId": 123,
    "suggestedBranchId": 2,
    "suggestedBranchName": "SmartWash Quận 7",
    "suggestedSlotId": 10,
    "suggestedTime": "2026-07-23T08:00:00Z",
    "expiresAt": "2026-07-23T08:05:00Z"
  }
}
```

Không có suggestion hợp lệ:

```json
{
  "statusCode": 200,
  "message": "Success",
  "data": null
}
```

### Handle Keep

```json
{
  "statusCode": 200,
  "message": "Overload suggestion handled successfully.",
  "data": {
    "success": true,
    "decision": "Keep",
    "message": "You have chosen to keep your current booking and wait.",
    "updatedBooking": {},
    "voucher": null,
    "refund": null
  }
}
```

### Handle Switch

```json
{
  "statusCode": 200,
  "message": "Overload suggestion handled successfully.",
  "data": {
    "success": true,
    "decision": "Switch",
    "message": "Switched to new branch successfully.",
    "updatedBooking": {},
    "voucher": {
      "voucherId": 99,
      "code": "OVL-ABC123",
      "discountAmount": 50000,
      "expiryDate": "2026-08-23T08:00:00Z",
      "isActive": true
    },
    "refund": null
  }
}
```

### Handle Cancel

```json
{
  "statusCode": 200,
  "message": "Overload suggestion handled successfully.",
  "data": {
    "success": true,
    "decision": "Cancel",
    "message": "Booking cancelled due to overload.",
    "updatedBooking": {},
    "voucher": null,
    "refund": {
      "refundedAmount": 150000,
      "refundDestination": "Wallet",
      "refundedPoints": 100,
      "restoredVoucherId": 20
    }
  }
}
```

### Error contract

```json
{
  "statusCode": 409,
  "message": "The overload suggestion has expired.",
  "details": null
}
```

Frontend cần có thể phân biệt tối thiểu:

- Suggestion hết hạn.
- Suggestion đã xử lý.
- Booking không còn Pending.
- Destination slot đã đầy.

Nên bổ sung machine-readable `errorCode`, ví dụ:

```json
{
  "statusCode": 409,
  "errorCode": "OVERLOAD_SUGGESTION_EXPIRED",
  "message": "The overload suggestion has expired."
}
```

---

## 11. P2 — Migration rollback one-to-many chưa an toàn

Migration Up bỏ unique index `BookingId` là đúng.

Migration Down hiện tạo lại:

```text
UNIQUE IX_OverloadSuggestions_BookingId
```

Sau khi production đã có nhiều suggestion lịch sử cho cùng booking, Down migration sẽ fail vì dữ liệu trùng `BookingId`.

Cần một trong các cách:

- Xác định migration không hỗ trợ rollback và ghi chú rõ.
- Trong Down, dọn dữ liệu theo policy trước khi tạo lại unique index.
- Không rollback về one-to-one sau khi hệ thống đã ghi dữ liệu one-to-many.

---

## 12. Automated tests bắt buộc

Backend hiện chưa có test project. Cần thêm integration tests ít nhất cho các case sau.

### Tạo suggestion

1. Chi nhánh không quá tải: không tạo suggestion.
2. Quá tải và có branch/slot phù hợp: tạo đúng một suggestion.
3. Đã có active suggestion: không tạo thêm.
4. Suggestion cũ hết hạn: có thể tạo suggestion mới.
5. Chạy trigger đồng thời nhiều lần: chỉ một active suggestion.
6. FCM failure không làm mất suggestion đã commit.

### GET suggestion

1. Đúng user, pending và còn hạn: trả suggestion.
2. Khác user: không lộ suggestion.
3. Hết hạn: trả `data: null`.
4. Đã processed: trả `data: null`.
5. Booking không Pending: trả `data: null`.
6. ScheduledTime đã qua: trả `data: null`.

### Handle contract

1. `{"decision":"Keep"}` thành công.
2. `{"decision":"Switch"}` thành công.
3. `{"decision":"Cancel"}` thành công.
4. Không cần gửi `suggestionId`.
5. Decision không hợp lệ: `400`.
6. Suggestion expired/processed: `409`.
7. Booking khác user: `404`.

### Concurrency

1. Hai request Switch đồng thời: một thành công, một `409`.
2. Switch và Cancel đồng thời: chỉ một decision được commit.
3. Hai request Cancel đồng thời: chỉ hoàn tiền/điểm/voucher một lần.
4. Hai booking cùng nhận slot cuối: chỉ một booking chuyển thành công.

### Switch

1. Trừ đúng capacity slot cũ.
2. Cộng đúng capacity slot mới.
3. Slot mới đầy: rollback toàn bộ.
4. Branch/time sau switch đúng dữ liệu suggestion trong DB.
5. Voucher được phát hành đúng một lần và có trong response.

### Cancel

1. Booking unpaid: refund amount bằng 0.
2. Wallet payment: hoàn đúng số tiền.
3. PayOS payment: hoàn theo destination đã công bố.
4. Điểm được hoàn đúng một lần.
5. Voucher được khôi phục đúng một lần.
6. Refund transaction có reference booking.

---

## 13. Thứ tự sửa đề xuất

1. Xóa `SuggestionId` khỏi Handle request DTO và sửa query theo `bookingId`.
2. Xóa/disable endpoint `accept-relocation` cũ.
3. Nối manager scan endpoint vào `OverloadSuggestionService` mới.
4. Bỏ fire-and-forget dùng scoped DbContext.
5. Làm atomic việc kiểm tra/tạo active suggestion.
6. Hoàn thiện concurrency response và idempotency refund/voucher.
7. Cập nhật `FE_API_Docs.md` đúng payload chỉ có `decision`.
8. Thêm integration/concurrency tests.
9. Chạy migration trên bản sao database có dữ liệu thật.
10. Gửi cho frontend Swagger/OpenAPI hoặc Postman collection đã xác nhận.

---

## 14. Definition of Done

Backend chỉ được xem là hoàn thành khi:

- Frontend gửi đúng `{"decision":"Switch|Cancel|Keep"}` mà không cần ID/branch/slot/time trong body.
- Không còn endpoint customer nào cho phép tự chọn destination branch/voucher.
- Manager scan hoặc check-in trigger thực sự tạo suggestion và gửi notification của flow mới.
- Một booking chỉ có tối đa một active suggestion tại một thời điểm.
- Trigger không dùng disposed/scoped DbContext ngoài request lifetime.
- Hai decision đồng thời chỉ có một request được commit.
- Switch cập nhật capacity atomically và trả voucher.
- Cancel hoàn tiền, điểm và voucher đúng một lần.
- GET không trả suggestion expired/processed.
- Các conflict trả `409`, không trả `500` ngoài dự kiến.
- Migration Up chạy thành công trên database có dữ liệu FCM token trùng.
- Có integration tests cho trigger, GET, Keep, Switch, Cancel và concurrency.
- `FE_API_Docs.md` và Swagger khớp implementation thực tế.

Cho đến khi các mục P0 hoàn tất, frontend không nên khóa implementation theo backend hiện tại vì request contract và cách kích hoạt suggestion vẫn còn thay đổi.

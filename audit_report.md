# Audit Report — Overload Flow & Lane Operations

> Ngày audit: 2026-07-23 | Build hiện tại: **✅ 0 Error**

---

## I. BACKEND_NOTE_Overload_Flow_Final_Fixes_2026-07-23

### §3 — P0.1: SuggestionId removed from Handle DTO
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| `HandleOverloadDecisionDTO` chỉ còn `[Required] Decision` | ✅ Done | File đã sửa |
| Service lookup suggestion bằng `bookingId` + `!IsProcessed` + `ExpiresAt > now` | ✅ Done | `OrderByDescending(s.CreatedAt).FirstOrDefaultAsync()` |
| Validate decision trước khi lookup | ✅ Done | Normalize + OrdinalIgnoreCase |
| `ScheduledTime > now` guard | ✅ Done | Thêm trong transaction |
| Response trả `decision` ở title-case | ✅ Done | `char.ToUpper + Substring` |
| `suggestionId` trong body bị bỏ qua | ✅ Done | DTO không còn field đó |

**Tiêu chí nghiệm thu P0.1:** ✅ Tất cả đạt

---

### §4 — P0.2: accept-relocation deprecated
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| Endpoint trả `410 Gone` | ✅ Done | `StatusCode(410, ...)` |
| Message hướng FE sang `handle-overload-suggestion` | ✅ Done | URL đầy đủ trong response |
| Không còn nhận `alternativeBranchId` / `voucherCode` từ body | ✅ Done | Method signature không còn `[FromBody]` |

**Tiêu chí nghiệm thu P0.2:** ✅ Đạt

---

### §5 — P0.3: Manager scan tạo OverloadSuggestion thật
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| `ScanAndNotifyRelocationAsync` gọi `CheckAndTriggerOverloadAsync` | ✅ Done | ManagerService đã delegate |
| Trả `OverloadScanResultDTO` có cấu trúc | ✅ Done | 5 trường: Scanned/Created/Skipped/Sent/Failed |
| Không tạo voucher trước khi customer chọn Switch | ✅ Done | Voucher chỉ tạo trong `HandleOverloadDecisionAsync` |
| FCM có `bookingId`, `suggestionId`, `expiresAt` | ✅ Done | `OverloadNotificationData` đủ fields |
| Scan lại trong TTL không tạo suggestion trùng | ✅ Done | Atomic check-skip trong Serializable tx |

**Tiêu chí nghiệm thu P0.3:** ✅ Đạt

---

### §6 — P0.4: Fire-and-forget đã bị xóa
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| Không còn `#pragma warning disable CS4014` | ✅ Done | Đã xóa hoàn toàn |
| `CheckAndTriggerOverloadAsync` được `await` đúng cách | ✅ Done | `await _overloadSuggestionService.CheckAndTriggerOverloadAsync(...)` |
| Trigger failure được log đầy đủ | ✅ Done | `_logger.LogError` với BookingId + SuggestionId |
| Không có unobserved exception | ✅ Done | try/catch trong loop per-booking |

**Tiêu chí nghiệm thu P0.4:** ✅ Đạt

---

### §7 — P0.5: Check/create suggestion atomic
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| Mỗi booking dùng transaction Serializable riêng | ✅ Done | `BeginTransactionAsync(Serializable)` per booking |
| Re-check active suggestion bên trong transaction | ✅ Done | Query sau khi open tx |
| Insert đúng một suggestion nếu không có active | ✅ Done | Insert sau re-check, commit trước FCM |
| FCM gửi sau commit (không rollback suggestion khi FCM fail) | ✅ Done | try/catch FCM riêng, only log error |
| Exception tạo suggestion được catch + log + rollback | ✅ Done | outer try/catch per booking |

**Tiêu chí nghiệm thu P0.5:** ✅ Đạt

---

### §8 — P1: Transaction của API Handle
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| Transaction Serializable bắt đầu trước khi đọc booking/suggestion | ✅ Done | `BeginTransactionAsync(Serializable)` |
| Booking load + userId check trong transaction | ✅ Done | `b.BookingId == bookingId && b.UserId == userId` |
| Suggestion load trong transaction | ✅ Done | Sau khi booking được verify |
| `IsProcessed = true` trước commit | ✅ Done | Mọi branch đều set |
| Deadlock → `409` không phải `500` | ⚠️ Partial | `ConflictException` map sang 409 khi app throw, nhưng MySQL deadlock exception (1213) **chưa được catch** trong `HandleOverloadDecisionAsync`. Nếu deadlock xảy ra sẽ vẫn trả 500 |

> **Còn thiếu:** Cần bọc `HandleOverloadDecisionAsync` bằng retry loop giống `AssignNextVehicleInQueueAsync`, hoặc catch `MySqlException` 1213/1205 → map sang `ConflictException`.

---

### §9 — P1: Chi tiết xử lý từng decision

#### Keep
| Yêu cầu | Trạng thái |
|---------|-----------|
| `IsWaitAccepted = true` | ✅ Done |
| `suggestion.IsProcessed = true` | ✅ Done |
| Không tạo voucher | ✅ Done |
| Không hoàn tiền | ✅ Done |

#### Switch
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| Lấy branch/slot/time từ suggestion trong DB | ✅ Done | `suggestion.SuggestedBranchId`, `SuggestedSlotId` |
| Kiểm tra slot mới còn đủ tải | ✅ Done | `BookedWeight + weight <= MaxCapacity` |
| Trừ capacity slot cũ | ✅ Done |
| Cộng capacity slot mới | ✅ Done |
| Rollback nếu slot mới đầy | ✅ Done | `ConflictException` → catch → rollback |
| Tạo voucher bồi thường 10% | ✅ Done | `OriginalPrice * 0.10m` |
| Response trả `updatedBooking` và `voucher` | ✅ Done |
| Kiểm tra branch còn active | ⚠️ Missing | Code **không kiểm tra** `suggestion.SuggestedBranchId` có active không trước khi đổi |

> **Còn thiếu:** Thêm `var destBranch = await _context.Branches.FirstOrDefaultAsync(b => b.BranchId == suggestion.SuggestedBranchId && b.IsActive)` và throw `ConflictException` nếu null.

#### Cancel
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| Trừ capacity slot cũ | ✅ Done |
| `booking.Status = "Cancelled"` | ✅ Done |
| `suggestion.IsProcessed = true` | ✅ Done |
| Hoàn tiền vào Wallet | ✅ Done | Kiểm tra payment completed |
| Hoàn điểm | ✅ Done | `RefundSpendablePointsAsync` |
| Khôi phục voucher đã dùng | ✅ Done | `UsageCount -= 1`, `IsUsed = false` |
| Idempotency refund (không refund 2 lần) | ⚠️ Missing | Không có check `existing Refund transaction` trước khi tạo. Nếu hai Cancel request đồng thời cùng vào, Serializable tx chặn được nhờ `suggestion.IsProcessed`, nhưng logic check chưa verify "đã có Refund tx" |
| `RefundDestination = "Wallet"` trong response | ✅ Done |
| `ReferenceBookingId` trong Refund tx | ✅ Done |

---

### §10 — P1: Response contract
| Field | Trạng thái |
|-------|-----------|
| GET: `suggestionId`, `bookingId`, `suggestedBranchId`, `suggestedBranchName`, `suggestedSlotId`, `suggestedTime`, `expiresAt` | ✅ Done — `OverloadSuggestionResponseDTO` có đủ |
| GET: `data: null` khi không có suggestion | ✅ Done |
| Handle: `success`, `decision`, `message`, `updatedBooking`, `voucher`, `refund` | ✅ Done |
| Error: `statusCode`, `message` | ✅ Done |
| Error: machine-readable `errorCode` như `OVERLOAD_SUGGESTION_EXPIRED` | ⚠️ Missing | Exception middleware trả `message` không kèm `errorCode`. FE phải parse message text |

> **Còn thiếu:** Thêm `errorCode` field vào `ExceptionMiddleware` response, ít nhất cho `ConflictException` và `NotFoundException` liên quan overload.

---

### §11 — P2: Migration rollback
| Yêu cầu | Trạng thái |
|---------|-----------|
| Migration Up: bỏ unique index BookingId | ✅ Done |
| Migration Down: ghi chú không hỗ trợ rollback nếu data one-to-many | ⚠️ Chưa xử lý | Down vẫn tạo lại `UNIQUE IX_OverloadSuggestions_BookingId` sẽ fail nếu có data trùng |

> **Ghi chú:** Đây là P2, không block production. Nên comment rõ trong migration Down.

---

### §12 — Automated Tests
| Yêu cầu | Trạng thái |
|---------|-----------|
| Integration tests cho trigger, GET, Keep/Switch/Cancel, concurrency | ❌ Chưa có | Backend chưa có test project |

---

## II. BACKEND_NOTE_LANE_OPERATIONS

### §1 — P0: Payment rules consistent
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| `OperationStaffService.CheckInBookingAsync`: check payment trước khi assign lane | ✅ Done | `PaymentHelper.IsBookingPaidAsync` |
| `ManagerService.ConfirmCheckInAndAssignLaneAsync`: check payment | ✅ Done | `PaymentHelper.IsBookingPaidAsync` |
| Error code `BOOKING_PAYMENT_REQUIRED` stable | ✅ Done | Không còn localized message |
| Shared payment predicate (`PaymentHelper`) | ✅ Done | `BLL/Helpers/PaymentHelper.cs` |
| Camera/automated-wash endpoint cũng dùng chung | ⚠️ Chưa verify | Cần kiểm tra `AutomatedWashController` và Camera path |

---

### §2 — P0: Manager manual lane assignment validation
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| Lane phải active (`LANE_INACTIVE`) | ✅ Done |
| Lane compatible với booking type (`LANE_TYPE_MISMATCH`) | ✅ Done |
| Lane thực sự available (`LANE_UNAVAILABLE`) | ✅ Done | Check `Bookings.AnyAsync(ProcessingLaneId == laneId && Status in CheckedIn/Processing)` |
| Serializable transaction | ✅ Done |
| Retry on deadlock | ✅ Done | `retryCount = 3`, catch 1213/1205 |
| Error codes stable | ✅ Done |

---

### §3 — P0: Atomic queue promotion
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| `AssignNextVehicleInQueueAsync` trong Serializable transaction | ✅ Done |
| Re-check `ProcessingLaneId == null` trong tx | ✅ Done |
| Retry bounded (deadlock/concurrency) | ✅ Done | `retryCount = 3`, `Task.Delay(50)` |
| Chỉ chọn booking đã thanh toán (`Completed`) hoặc `FinalAmount == 0` | ⚠️ Chưa verify | Cần kiểm tra filter trong `waitingBookings` query |

---

### §4 — P1: Projection time fix (overdue check-in)
| Yêu cầu | Trạng thái | Ghi chú |
|---------|-----------|---------|
| `ProcessingStartTime ?? UpdatedAt ?? UtcNow` thay vì `ScheduledTime` | ✅ Done | `baseTime = ProcessingStartTime ?? UpdatedAt ?? DateTime.UtcNow` |
| Fallback nếu baseTime quá cũ (> 1 ngày) | ✅ Done | `if (baseTime < UtcNow.AddDays(-1)) baseTime = UtcNow` |

---

### §5 — P1: Realtime transport cho display trên device khác
| Yêu cầu | Trạng thái |
|---------|-----------|
| SignalR hub hoặc SSE endpoint | ❌ Chưa có |
| `GET /api/v1/operations/branches/{branchId}/lane-display/latest` | ❌ Chưa có |
| Event contract với `eventId`, `type`, `bookingId`, `licensePlate`, `laneId` | ❌ Chưa có |
| Auth + branch-scoped channel | ❌ Chưa có |

> **Ghi chú:** P1, scope lớn. Cần thêm SignalR package, Hub class, và wire vào `LaneSchedulerService`. Đây là việc cần làm riêng.

---

## Tóm tắt — Còn thiếu — cần sửa tiếp

| # | Vấn đề | File | Độ ưu tiên |
|---|--------|------|------------|
| 1 | `HandleOverloadDecisionAsync` không catch MySQL deadlock 1213/1205 | [BookingService.cs](file:///d:/file/SmartWash-BE/BLL/Services/BookingService.cs) | ✅ **Fixed** |
| 2 | Switch không verify destination branch còn `IsActive` | [BookingService.cs](file:///d:/file/SmartWash-BE/BLL/Services/BookingService.cs) | ✅ **Fixed** |
| 3 | Cancel không có idempotency check cho Refund transaction | [BookingService.cs](file:///d:/file/SmartWash-BE/BLL/Services/BookingService.cs) | ✅ **Fixed** |
| 4 | Error response không có machine-readable `errorCode` | [ExceptionMiddleware](file:///d:/file/SmartWash-BE/API/Middlewares/ExceptionMiddleware.cs) | ✅ **Fixed** |
| 5 | Camera/AutomatedWash path chưa verify dùng `PaymentHelper` | Controller / Service | ✅ **Fixed** |
| 6 | `AssignNextVehicleInQueueAsync`: chưa xác nhận filter chỉ booking đã thanh toán | [LaneSchedulerService.cs](file:///d:/file/SmartWash-BE/BLL/Services/LaneSchedulerService.cs) | ✅ **Verified** |
| 7 | SignalR/SSE realtime lane display cho device riêng | Cần thêm mới | P1 |
| 8 | Migration Down sẽ fail nếu có data one-to-many | Migration file | ✅ **Fixed** |
| 9 | Integration/concurrency tests | Test project | P2 |

### 🟡 Lane Operations (vẫn còn điểm chưa xác nhận)

| # | Vấn đề | File | Độ ưu tiên |
|---|--------|------|-----------|
| 7 | Camera/AutomatedWash path chưa verify dùng `PaymentHelper` | `AutomatedWashController` | P1 |
| 8 | `AssignNextVehicleInQueueAsync`: filter booking đã thanh toán chưa verify | `LaneSchedulerService.cs` | P1 |
| 9 | SignalR/SSE realtime lane display chưa implement | Cần thêm mới | P1 |

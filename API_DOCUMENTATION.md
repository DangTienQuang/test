# Tài Liệu API AutoWashPro (Backend-Driven Architecture)

Tài liệu này hướng dẫn cách Frontend tích hợp với Backend API, theo chuẩn thiết kế "Smart Filter" - đưa logic tính toán nặng xuống Server để Frontend nhẹ nhất có thể.

Mỗi API/User Flow đều được ghi chép theo cấu trúc 6 phần:
1. Business Purpose (Mục đích nghiệp vụ)
2. Prerequisites (Điều kiện tiên quyết)
3. Request Payload (Dữ liệu gửi đi)
4. Expected Response & Error Handling (Dữ liệu trả về & Xử lý lỗi)
5. Next Steps (Bước tiếp theo)
6. Critical Warnings for FE (Cảnh báo quan trọng cho Frontend)

---

## 1. User Flow: Kiểm tra Lịch Khả dụng (Check Available Slots)
Quy trình đặt lịch của hệ thống tuân theo chuẩn **Cart-first, Slot-second** (Chọn giỏ hàng trước, chọn lịch sau).

### 1. Business Purpose
API này giúp Frontend lấy ra danh sách các khung giờ (Time Slots) của một ngày cụ thể. Dựa vào tổng "Trọng lượng" (`CapacityWeight`) của các xe và dịch vụ mà khách hàng đã chọn (giỏ hàng), Backend sẽ tự động tính toán, lọc, và quyết định xem khung giờ nào còn đủ chỗ trống để Frontend hiển thị trạng thái `IsAvailable` (Cho phép bấm chọn hay không).

### 2. Prerequisites
- Người dùng **phải đăng nhập** thành công (có Token hợp lệ).
- Người dùng **phải chọn xong** danh sách xe và dịch vụ cần đặt (giỏ hàng không được rỗng) **trước khi** vào màn hình chọn ngày giờ.

### 3. Request Payload
- **Endpoint**: `POST /api/v1/bookings/available-slots`
- **Method**: `POST` (Dùng POST để gửi body giỏ hàng)
- **Headers**: `Authorization: Bearer <token>`
- **Body (`CheckAvailableSlotsRequestDTO`)**:
```json
{
  "targetDate": "2026-06-02T00:00:00Z", // Ngày khách muốn đặt (Frontend nên parse chuẩn ISO)
  "bookingVehicles": [
    {
      "licensePlate": "29A-12345",
      "vehicleTypeId": 1,
      "serviceId": 2,
      "price": 150000
    },
    {
      "licensePlate": "30B-67890",
      "vehicleTypeId": 2,
      "serviceId": 3,
      "price": 200000
    }
  ]
}
```

### 4. Expected Response & Error Handling
**Thành công (200 OK)**:
Trả về danh sách tất cả các Slot của xưởng. Nếu một Slot không đủ chỗ trống hoặc đã quá giờ, Backend sẽ tự set `IsAvailable = false` và kèm theo lý do để Frontend render.
```json
{
  "statusCode": 200,
  "message": "Success",
  "data": [
    {
      "slotId": 1,
      "timeRange": "08:00 - 09:00",
      "isAvailable": false,
      "reason": "Đã qua giờ" // Hoặc "Đã kín", "Không đủ sức chứa cho giỏ hàng của bạn", "Chỉ dành cho VIP"
    },
    {
      "slotId": 2,
      "timeRange": "09:00 - 10:00",
      "isAvailable": true,
      "reason": "Trống"
    }
  ]
}
```

**Các lỗi phổ biến (4xx, 5xx)**:
- `400 Bad Request`: Múi giờ đặt lịch vi phạm quy định về `BookingWindowDays` (Ví dụ: Khách chỉ được đặt trước 3 ngày nhưng truyền `TargetDate` 1 tháng sau).
- `404 Not Found`: Không tìm thấy thông tin Profile/Hạng thành viên của user.
- `401 Unauthorized`: Token hết hạn hoặc chưa đăng nhập.

### 5. Next Steps
- Dựa trên mảng `data` trả về, Frontend render danh sách các nút bấm khung giờ.
- Những slot có `isAvailable == false`, Frontend cần disable nút bấm (grey-out) và hiển thị tooltip/text `reason` ngay bên dưới để khách hiểu vì sao không chọn được.
- Khi khách chọn 1 slot có `isAvailable == true`, Frontend lưu lại `slotId` vào State và tiến hành gọi API tiếp theo: `POST /api/v1/bookings/check-compatibility` hoặc `POST /api/v1/bookings` để xác nhận đặt lịch.

### 6. Critical Warnings for FE (Cảnh báo Quan trọng)
- 🔴 **Tuyệt đối không tự lọc/tính toán (No Client-side Filtering)**: Frontend **KHÔNG ĐƯỢC** tự tính tổng số lượng xe rồi trừ vào Capacity. Toàn bộ logic kiểm tra "giờ đã qua", "đủ chỗ không", "có phải VIP không" phải phụ thuộc 100% vào giá trị `isAvailable` do Backend trả về.
- 🔴 **Luôn gọi lại API khi giỏ hàng thay đổi**: Nếu ở màn hình chọn giờ mà khách ấn nút "Back" ra ngoài để thêm/bớt một chiếc xe, thì khi vào lại màn hình chọn giờ, Frontend **bắt buộc phải gọi lại API này** với payload mới. (Ví dụ: Slot 09:00 ban đầu `isAvailable=true` vì khách chỉ rửa 1 xe, nhưng nếu thêm 3 xe nữa vào giỏ thì slot đó có thể chuyển thành `false`).
- 🔴 **Timezone (Múi giờ)**: Backend đã tự động chuẩn hoá thời gian so sánh theo múi giờ Việt Nam (UTC+7). FE truyền `targetDate` lên tốt nhất là truyền chuỗi ngày thuần tuý theo UTC (như `2026-06-02T00:00:00.000Z`) để tránh lệch ngày.

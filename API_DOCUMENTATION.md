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

---

## 2. User Flow: Nhân viên Check-in / Check-out bằng Biển số xe

### 1. Business Purpose
Thay vì nhân viên phải ghi nhớ mã Booking ID, API này cho phép nhân viên (Staff/Admin) cập nhật trạng thái lịch hẹn (Check-in để nhận xe, hoặc Complete để trả xe) bằng cách nhập trực tiếp hoặc quét **biển số xe** (License Plate). Backend sẽ tự động quét danh sách lịch hẹn hợp lệ trong ngày hôm nay (theo múi giờ Việt Nam) để cập nhật.

### 2. Prerequisites
- Frontend (App nội bộ của Staff) đang ở màn hình quét biển số hoặc có ô textbox nhập biển số.
- API chỉ được gọi bởi tài khoản có quyền `Admin` hoặc `Staff`.

### 3. Request Payload
- **Endpoint**: `PUT /api/v1/admin/bookings/status-by-license-plate`
- **Method**: `PUT`
- **Headers**: `Authorization: Bearer <staff_token>`
- **Body (`UpdateBookingStatusByPlateDTO`)**:
```json
{
  "licensePlate": "29A-123.45", // Có thể chứa dấu gạch ngang, khoảng trắng, chấm. Backend sẽ tự lọc lấy chữ và số để so khớp.
  "newStatus": "CheckedIn"      // Hoặc "Completed"
}
```

### 4. Expected Response & Error Handling
**Thành công (200 OK)**:
Sau khi cập nhật thành công, API trả về bản tóm tắt thông tin của Booking đó để Staff hiển thị lên màn hình (xác nhận lại xe này sử dụng dịch vụ gì, khách hàng là ai).
```json
{
  "statusCode": 200,
  "message": "Đã cập nhật trạng thái xe 29A-123.45 thành: CheckedIn",
  "data": {
    "bookingId": 105,
    "licensePlate": "29A12345",
    "serviceName": "Rửa xe bọt tuyết, Hút bụi nội thất",
    "scheduledTime": "2026-06-02T08:00:00Z",
    "status": "CheckedIn",
    "originalPrice": 150000,
    "pointDiscountAmount": 0,
    "voucherDiscountAmount": 0,
    "finalAmount": 150000
  }
}
```

**Các lỗi phổ biến (4xx, 5xx)**:
- `400 Bad Request`: Biển số xe rỗng, hoặc `newStatus` gửi lên không hợp lệ (Không phải CheckedIn/Completed).
- `404 Not Found`: "Không tìm thấy lịch hẹn hợp lệ trong ngày hôm nay cho xe có biển số X". Nguyên nhân có thể do:
  - Khách chưa đặt lịch cho ngày hôm nay.
  - Xe đã được Check-in rồi nhưng Staff lại bấm gọi "CheckedIn" lần nữa (Nếu đã CheckedIn thì chỉ được chuyển lên Completed).

### 5. Next Steps
- Dựa vào kết quả trả về, hiển thị thông báo thành công xanh lá cây (Toast/Alert) cho Staff.
- Render lại thông tin xe (Tên dịch vụ, giá tiền) lên màn hình của Staff để Staff tiến hành thi công.

### 6. Critical Warnings for FE (Cảnh báo Quan trọng)
- 🔴 **FE không cần chuẩn hóa biển số**: Không cần viết Regex phức tạp ở phía Client để xóa khoảng trắng hay dấu chấm. Cứ gửi nguyên chuỗi biển số khách/staff nhập lên, Backend đã có hàm `NormalizeLicensePlate` lo việc chuẩn hóa.
- 🔴 **Giới hạn thời gian**: API này được thiết kế để tìm kiếm Booking **chỉ trong ngày hôm nay** (dựa theo thời gian thực của VN). Nếu nhân viên cố tình test bằng cách nhập biển số của lịch hẹn ngày mai, Backend sẽ từ chối và báo 404.

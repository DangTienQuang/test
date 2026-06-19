# Hướng Dẫn Sử Dụng Tính Năng "Crowdsourcing Dòng Xe" Cho Frontend

Tính năng này cho phép người dùng đóng góp dòng xe mới vào hệ thống khi dòng xe của họ chưa có trong danh sách. Dòng xe được đóng góp sẽ vào trạng thái "Chờ duyệt" (Pending) và không hiển thị công khai, nhưng người dùng vẫn có thể liên kết nó vào phương tiện của mình ngay lập tức. Sau đó, nhân viên (Staff/Admin) sẽ kiểm tra, chọn loại xe (VehicleTypeId) và phê duyệt để nó trở thành dữ liệu chung.

Dưới đây là các API mà Frontend (FE) cần gọi theo luồng người dùng và luồng nhân viên.

---

## 1. Luồng Người Dùng (Customer) - Đóng Góp Dòng Xe & Thêm Phương Tiện

### Bước 1.1: Gửi yêu cầu thêm dòng xe mới (Khi người dùng chọn "Khác" hoặc nhập tay)
**API:** `POST /api/v1/carmodels/request`
- **Yêu cầu (Headers):** Cần có token xác thực (Bearer Token).
- **Body:**
```json
{
  "brand": "VinFast",
  "name": "VF9",
  "vehicleTypeId": 3 // (Tùy chọn) Có thể null nếu người dùng không chắc chắn, nhưng khuyến khích truyền nếu FE có thể lấy được từ UI
}
```
- **Phản hồi (Response):**
```json
{
  "statusCode": 200,
  "message": "Yêu cầu thêm dòng xe đã được gửi và đang chờ duyệt.",
  "data": 15 // Trả về CarModelId của dòng xe vừa tạo
}
```

### Bước 1.2: Sử dụng ID vừa tạo để thêm phương tiện (Vehicle) mới vào gara
**Lưu ý quan trọng:** FE cần lấy `data` (tức là `CarModelId` mới) từ API trên và truyền ngay vào API tạo phương tiện của hệ thống.
**API:** `POST /api/v1/vehicles` (Dạng FormData)
- **Các trường FormData cần chú ý:**
  - `LicensePlate`: Biển số xe
  - `VehicleTypeId`: Loại xe (Bắt buộc theo yêu cầu cũ)
  - `CarModelId`: Trền vào `CarModelId` vừa nhận được (VD: `15`)
  - (Không cần truyền trường chuỗi `CarModel` nữa vì đã có `CarModelId`)

---

## 2. Luồng Nhân Viên (Staff/Admin) - Phê Duyệt / Từ Chối

Nhân viên sẽ có một trang quản lý các dòng xe đang chờ duyệt (Pending).

### Bước 2.1: Lấy danh sách các dòng xe đang chờ duyệt
**API:** `GET /api/v1/admin/carmodels/pending`
- **Yêu cầu (Headers):** Token của role Admin hoặc Staff.
- **Phản hồi (Response):** Trả về mảng các xe có `status` = `"Pending"`.

### Bước 2.2: Phê duyệt dòng xe
Nhân viên chọn đúng `VehicleTypeId` chuẩn cho dòng xe đó rồi nhấn "Duyệt".
**API:** `PUT /api/v1/admin/carmodels/{id}/approve` (Trong đó `{id}` là CarModelId)
- **Yêu cầu (Headers):** Token của role Admin hoặc Staff.
- **Body (Bắt buộc):**
```json
{
  "vehicleTypeId": 3 // Bắt buộc Staff phải chốt xem xe này là Sedan, SUV,... để tính giá chuẩn
}
```

### Bước 2.3: Từ chối dòng xe (Nếu user nhập sai/spam)
**API:** `PUT /api/v1/admin/carmodels/{id}/reject`
- **Yêu cầu (Headers):** Token của role Admin hoặc Staff.
- **Body:** Không cần body.

---

## Lưu ý về danh sách thả xuống (Dropdown)
- Danh sách dòng xe tại API `GET /api/v1/carmodels` (dùng cho dropdown) sẽ tự động **chỉ trả về các xe đã "Approved" (Đã duyệt)**. FE không cần phải filter trạng thái ở client.

🚀 TÀI LIỆU HƯỚNG DẪN DỰ ÁN SMARTWASH BE (Dành cho Team FE & BE)
Chào mừng các bạn đến với hệ thống SmartWash! Tài liệu này sẽ hướng dẫn cách setup, chạy code và tích hợp API cho cả team Frontend và Backend.

📌 1. TỔNG QUAN HỆ THỐNG (TECH STACK)
Framework: .NET 8.0 Web API

Kiến trúc: 3 Tầng (API, BLL - Business Logic, DAL - Data Access)

Database: MySQL 8.4 (Cloud Database hosted tại Aiven.io)

Server Hosting: Render.com (Sử dụng Docker Alpine để tối ưu RAM)

Link API Live (Dùng cho FE test): https://smartwash-be.onrender.com/swagger

🛠 2. DÀNH CHO TEAM BACKEND (Cài đặt & Code)
Để code và chạy dự án trên máy cá nhân, các bạn làm theo các bước sau:

Bước 1: Cài đặt công cụ cơ bản

Cài đặt .NET 8.0 SDK.

Sử dụng Visual Studio 2022 (Khuyên dùng) hoặc VS Code / JetBrains Rider.

Cài phần mềm quản lý Database (MySQL Workbench hoặc DBeaver).

Bước 2: Clone Code & Cấu hình Database

Clone code từ nhánh main về máy.

Mở file AutoWashPro.sln bằng Visual Studio.

Mở file API/appsettings.json. Sửa lại chuỗi kết nối (DefaultConnection) trỏ về Database MySQL của team (có thể dùng DB Local ở máy bạn, hoặc dùng chung DB Aiven của dự án).
Ví dụ chuỗi kết nối local: Server=localhost;Database=SmartWash_DB;User=root;Password=123456;

Bước 3: Cập nhật Database (Migration)
Vì dự án dùng Entity Framework Core (Code-First), nên khi kéo code về, DB ở máy bạn chưa có bảng nào cả.

Mở Package Manager Console (Vào Tools -> NuGet Package Manager -> Package Manager Console).

Chọn ô Default project là DAL.

Gõ lệnh thần thánh:

Bash
Update-Database
(Lệnh này sẽ tạo toàn bộ bảng và nạp dữ liệu Admin mặc định vào Database của bạn).

Bước 4: Chạy Project
Nhấn nút Run (F5) trên Visual Studio (chọn cấu hình chạy là http hoặc https). Giao diện Swagger sẽ hiện ra ở localhost.

💻 3. DÀNH CHO TEAM FRONTEND (Cách Test & Tích hợp API)
Team FE không cần phải cài code Backend, chỉ cần gọi thẳng vào link API Live mà team BE đã deploy.

👉 Link tài liệu API (Swagger): https://smartwash-be.onrender.com/swagger

Hướng dẫn cách gọi API có bảo mật (Token):
Hệ thống sử dụng bảo mật JWT (JSON Web Token). Ngoại trừ API Đăng nhập/Đăng ký và Xem dịch vụ, các API khác đều bắt buộc phải có Token.

1. Lấy Token bằng tài khoản Admin (Đã được BE tạo sẵn):

Tìm API: POST /api/v1/auth/login

Nhấn Try it out, nhập thông tin sau:

JSON
{
  "phoneNumber": "0999999999",
  "password": "Admin@123"
}
Bấm Execute. Hệ thống sẽ trả về một chuỗi token rất dài. Hãy copy chuỗi token này.

2. Gắn Token vào Swagger để test các API khác:

Kéo lên đầu trang Swagger, bấm vào nút "Authorize" (Hình ổ khóa màu xanh lá).

Ở ô Value, bạn chỉ cần dán chuỗi token vừa copy vào (Không cần gõ chữ Bearer đằng trước, BE đã cấu hình tự động thêm vào rồi).

Bấm Authorize -> Close.

Giờ thì bạn có thể test bất kỳ API nào (Ví dụ: GET /api/v1/users/me).

☁️ 4. KIẾN TRÚC DEPLOYMENT (Dành cho Leader / DevOps)
Hệ thống đã được thiết lập CI/CD cơ bản (Tự động deploy).

1. Quản lý Database (Aiven.io)

DB đang chạy trên gói Free của Aiven (Singapore/Asia Pacific).

Kết nối bằng MySQL Workbench qua thông số Service URI cung cấp trong Dashboard Aiven.

Lưu ý: Gói Cloud cho phép mọi IP truy cập (0.0.0.0/0).

2. Quản lý Server API (Render.com)

Dự án được host trên Web Service của Render (Gói Free).

Mỗi khi có code mới được git push lên nhánh main, Render sẽ tự động kéo code về, đọc file Dockerfile và Build lại hệ thống. Không cần thao tác bằng tay!

Các biến môi trường (Environment Variables) đã thiết lập trên Render:

ASPNETCORE_ENVIRONMENT = Development (Để mở giao diện Swagger trên Cloud).

ConnectionStrings__DefaultConnection = [Chuỗi kết nối Aiven] (Để ghi đè cấu hình local, ép code gọi lên DB Cloud).

Hạn chế của gói Free: Nếu 15 phút không có ai gọi API, server sẽ "ngủ đông". Khi FE gọi lại lần đầu sẽ bị delay mất khoảng 30-50 giây. Các lần gọi sau sẽ nhanh như bình thường.

Chúc team làm việc hiệu quả và ra mắt SmartWash thành công rực rỡ! 🚀
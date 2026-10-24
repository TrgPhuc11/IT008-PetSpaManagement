# Spa thú cưng

Ứng dụng WPF quản lý spa thú cưng (đồ án). Các màn hình hiện có được liệt kê ở mục "Màn hình đã có".

## Màn hình đã có
- **Đăng nhập**.
- **Trang chủ** (giữ chỗ, các chức năng khác sẽ bổ sung sau).
- **Quản lý nhân viên** (chỉ vai trò Quản lý): xem/tìm, thêm, sửa, đổi trạng thái Đang làm việc / Đã nghỉ việc, đặt lại mật khẩu. Không có chức năng xoá.

## Yêu cầu
- Visual Studio 2022 17.14+ hoặc Visual Studio 2026, có .NET 10 SDK và workload "Phát triển ứng dụng desktop .NET".
- SQL Server hoặc SQL Server LocalDB / Express.

## Cấu hình kết nối
Sửa chuỗi kết nối trong `src/SpaThuCung/appsettings.json` (khoá `ConnectionStrings:SpaThuCung`), ví dụ:

- LocalDB: `Server=(localdb)\\MSSQLLocalDB;Database=SpaThuCung;Trusted_Connection=True;TrustServerCertificate=True`
- SQL Express: `Server=.\\SQLEXPRESS;Database=SpaThuCung;Trusted_Connection=True;TrustServerCertificate=True`

(Trong file JSON phải viết hai dấu `\\`.)

## Cơ sở dữ liệu
Ứng dụng tự tạo CSDL `SpaThuCung` và bảng `NhanVien` khi chạy lần đầu (script `Database/01_TaoCSDL_NhanVien.sql`, chạy lại nhiều lần vẫn an toàn). Cũng có thể mở file này trong SSMS và chạy tay.

## Chạy
Mở `SpaThuCung.sln` trong Visual Studio, nhấn F5.

## Tài khoản mẫu (mật khẩu đều là `123456`)
| Tên đăng nhập | Vai trò | Trạng thái |
|---|---|---|
| quanly | Quản lý | Đang làm việc |
| nhanvien | Nhân viên | Đang làm việc |
| nghiviec | Nhân viên | Đã nghỉ việc (không đăng nhập được) |

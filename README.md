# Spa thú cưng

Ứng dụng WPF quản lý spa thú cưng (đồ án). Các màn hình hiện có được liệt kê ở mục "Màn hình đã có".

## Màn hình đã có
- **Đăng nhập**.
- **Trang chủ** (giữ chỗ, các chức năng khác sẽ bổ sung sau).
- **Dịch vụ & bảng giá** (mọi nhân viên xem và tra giá theo loài, cân nặng; chỉ Quản lý thêm, sửa, ngừng/mở lại dịch vụ và chỉnh bảng giá; mỗi lần chỉ sửa một dịch vụ, bấm Chỉnh sửa trên thẻ dịch vụ). Mỗi dòng bảng giá gồm loài, khoảng cân nặng, giá và thời lượng (phút); các khoảng cùng loài không được chồng lấn, khoảng tính từ trên "Từ" đến hết "Đến". Dịch vụ chưa có phiếu dịch vụ nào thì xoá được; đã có phiếu thì chỉ chuyển Ngừng cung cấp.
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
Ứng dụng tự tạo CSDL `SpaThuCung` và các bảng khi khởi động, bằng các script trong thư mục `Database/` chạy theo thứ tự tên (chạy lại nhiều lần vẫn an toàn):

- `01_TaoCSDL_NhanVien.sql`: CSDL, bảng `NhanVien` và tài khoản mẫu.
- `02_TaoBang_DichVu_BangGia.sql`: bảng `DichVu`, `BangGia` và dữ liệu mẫu (chỉ thêm khi chưa có dịch vụ nào).

Cũng có thể mở các file này trong SSMS và chạy tay theo đúng thứ tự.

## Chạy
Mở `SpaThuCung.sln` trong Visual Studio, nhấn F5.

## Tài khoản mẫu (mật khẩu đều là `123456`)
| Tên đăng nhập | Vai trò | Trạng thái |
|---|---|---|
| quanly | Quản lý | Đang làm việc |
| nhanvien | Nhân viên | Đang làm việc |
| nghiviec | Nhân viên | Đã nghỉ việc (không đăng nhập được) |

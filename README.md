# Spa thú cưng

Ứng dụng WPF quản lý spa thú cưng (đồ án). Các màn hình hiện có được liệt kê ở mục "Màn hình đã có".

## Màn hình đã có
- **Đăng nhập**.
- **Trang chủ** (giữ chỗ, các chức năng khác sẽ bổ sung sau).
- **Khách hàng** (mọi nhân viên): tìm theo tên hoặc SĐT, thêm, sửa; SĐT không trùng (nhập SĐT đã có thì mở hồ sơ cũ); hiện số lần không đến và cờ cảnh báo theo ngưỡng trong Cấu hình; xem thú cưng của khách. Không có xoá.
- **Thú cưng** (mọi nhân viên): tìm, lọc theo khách, loài, chưa tiêm phòng; thêm thú cưng cho khách, sửa tên, loài, cân nặng, ghi chú và xác nhận tiêm phòng. Không đổi chủ, không xoá.
- **Dịch vụ & bảng giá** (mọi nhân viên xem và tra giá theo loài, cân nặng; chỉ Quản lý thêm, sửa, ngừng/mở lại dịch vụ và chỉnh bảng giá; mỗi lần chỉ sửa một dịch vụ, bấm Chỉnh sửa trên thẻ dịch vụ). Mỗi dòng bảng giá gồm loài, khoảng cân nặng, giá và thời lượng (phút); các khoảng cùng loài không được chồng lấn, khoảng tính từ trên "Từ" đến hết "Đến". Dịch vụ chưa có phiếu dịch vụ nào thì xoá được; đã có phiếu thì chỉ chuyển Ngừng cung cấp.
- **Quản lý nhân viên** (chỉ vai trò Quản lý): xem/tìm, thêm, sửa, đổi trạng thái Đang làm việc / Đã nghỉ việc, đặt lại mật khẩu. Không có chức năng xoá.
- **Thống kê doanh thu** (chỉ Quản lý): chọn khoảng ngày (theo ngày hẹn của phiếu), xem theo ngày hoặc tháng; biểu đồ doanh thu và số phiếu mỗi kỳ, doanh thu theo dịch vụ hoặc theo Groomer, số phiếu theo trạng thái. Doanh thu chỉ tính phiếu "Đã thanh toán".

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
- `03_TaoBang_CauHinh_CaLamViec.sql`: bảng `CauHinh`, `CaLamViec` và giá trị mặc định.
- `04_TaoBang_LichLamViec.sql`: bảng `LichLamViec`.
- `05_TaoBang_PhieuDichVu.sql`: bảng `KhachHang`, `ThuCung`, `Long`, `PhieuDichVu`, `ChiTietPhieu` (chưa có dữ liệu).

Cũng có thể mở các file này trong SSMS và chạy tay theo đúng thứ tự.

Muốn có số liệu để thử màn Thống kê doanh thu (khi chưa làm màn Lập phiếu), chạy tay `Database/DuLieuMau/DuLieuMau_ThongKe.sql` trong SSMS: script thêm vài khách, thú cưng, 2 Groomer mẫu (`groomer1`, `groomer2`) và phiếu có ngày hẹn từ 75 ngày trước đến 5 ngày tới. Cuối file có đoạn xoá dữ liệu mẫu. Script này không chạy tự động.

## Chạy
Mở `SpaThuCung.sln` trong Visual Studio, nhấn F5.

## Tài khoản mẫu (mật khẩu đều là `123456`)
| Tên đăng nhập | Vai trò | Trạng thái |
|---|---|---|
| quanly | Quản lý | Đang làm việc |
| nhanvien | Nhân viên | Đang làm việc |
| nghiviec | Nhân viên | Đã nghỉ việc (không đăng nhập được) |

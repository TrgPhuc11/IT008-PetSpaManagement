# Spa thú cưng

Ứng dụng WPF quản lý spa thú cưng (đồ án). Các màn hình hiện có được liệt kê ở mục "Màn hình đã có".

## Màn hình đã có
- **Đăng nhập**.
- **Trang chủ** (giữ chỗ, các chức năng khác sẽ bổ sung sau).
- **Lập phiếu** (mọi nhân viên, UC10): tìm khách theo tên/SĐT hoặc thêm khách mới ngay trên form; chọn hoặc thêm thú cưng (chỉ thú cưng đã xác nhận tiêm phòng và chưa có phiếu đang hoạt động); khách có số lần Không đến từ ngưỡng cảnh báo thì hiện lưu ý (không chặn). Chọn dịch vụ: giá và thời lượng tra theo loài và cân nặng lúc lập; không có khoảng cân phù hợp hoặc trường hợp đặc biệt thì nhập tay kèm lý do. Đặt lịch: chọn ngày và mốc giờ 15 phút trong ca, Groomer có lịch đúng ngày đúng ca (có thể để Chưa phân công). Walk-in: nhận ngay, chọn lồng Walk-in trống và Groomer của ca hiện tại, phiếu ở "Đã nhận thú cưng". Vượt thời lượng ca của Groomer hoặc xong quá giờ được lố thì chỉ hỏi lại.
- **Khách hàng** (mọi nhân viên): tìm theo tên hoặc SĐT, thêm, sửa; SĐT không trùng (nhập SĐT đã có thì mở hồ sơ cũ); hiện số lần không đến và cờ cảnh báo theo ngưỡng trong Cấu hình; xem thú cưng của khách. Không có xoá.
- **Thú cưng** (mọi nhân viên): tìm, lọc theo khách, loài, chưa tiêm phòng; thêm thú cưng cho khách, sửa tên, loài, cân nặng, ghi chú và xác nhận tiêm phòng. Không đổi chủ, không xoá.
- **Dịch vụ & bảng giá** (mọi nhân viên xem và tra giá theo loài, cân nặng; chỉ Quản lý thêm, sửa, ngừng/mở lại dịch vụ và chỉnh bảng giá; mỗi lần chỉ sửa một dịch vụ, bấm Chỉnh sửa trên thẻ dịch vụ). Mỗi dòng bảng giá gồm loài, khoảng cân nặng, giá và thời lượng (phút); các khoảng cùng loài không được chồng lấn, khoảng tính từ trên "Từ" đến hết "Đến". Dịch vụ chưa có phiếu dịch vụ nào thì xoá được; đã có phiếu thì chỉ chuyển Ngừng cung cấp.
- **Quản lý nhân viên** (chỉ vai trò Quản lý): xem/tìm, thêm, sửa, đổi trạng thái Đang làm việc / Đã nghỉ việc, đặt lại mật khẩu. Không có chức năng xoá.
- **Thống kê doanh thu** (chỉ Quản lý): chọn khoảng ngày (theo ngày hẹn của phiếu), xem theo ngày hoặc tháng; biểu đồ doanh thu và số phiếu mỗi kỳ, doanh thu theo dịch vụ hoặc theo Groomer, số phiếu theo trạng thái. Doanh thu chỉ tính phiếu "Đã thanh toán".
- **Sao lưu & phục hồi** (chỉ Quản lý): chọn nơi lưu (nên là ổ đĩa ngoài/USB) rồi bấm Sao lưu ngay; mỗi lần tạo thư mục `SpaThuCung_SaoLuu_yyyyMMdd_HHmmss` gồm `SpaThuCung.bak` (SQL Server BACKUP), bản chép thư mục ảnh `HinhAnh` và `SaoLuu.json`. Phục hồi: chọn một bản trong danh sách hoặc một tệp `.bak`, xác nhận cảnh báo ghi đè; hệ thống tự giữ bản dữ liệu ngay trước khi phục hồi (`TruocPhucHoi.bak`), lỗi thì quay lại bản đó; xong phải đăng nhập lại. SQL Server đọc ghi tệp qua thư mục trung gian `C:\ProgramData\SpaThuCung\SaoLuuTam` (dịch vụ SQL Server thường không ghi thẳng được vào USB). Nơi lưu và lần sao lưu gần nhất ghi nhớ ở `%LocalAppData%\SpaThuCung\SaoLuu.json`. Tài khoản Windows kết nối SQL Server cần quyền sysadmin (máy cài SQL Express thì thường đã có).

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
- `06_TaoBang_NhatKy_HinhAnh_Long.sql`: bảng `NhatKyPhieu`, `HinhAnhPhieu` và 10 lồng mặc định (L01–L06 Đặt lịch, W01–W04 Walk-in; chỉ thêm khi bảng `Long` còn trống).

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

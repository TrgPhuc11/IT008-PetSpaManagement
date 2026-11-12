-- =====================================================================
-- Spa thú cưng - DỮ LIỆU MẪU để thử màn Thống kê doanh thu (UC18)
-- KHÔNG chạy tự động: mở trong SSMS và chạy tay khi muốn có số liệu thử.
-- Chạy sau khi ứng dụng đã tạo CSDL (hoặc sau các script 01 → 05).
-- Tạo 3 khách, 4 thú cưng, 4 lồng, 2 Groomer mẫu (mật khẩu 123456) và
-- phiếu dịch vụ có ngày hẹn từ 75 ngày trước đến 5 ngày tới.
-- Chỉ thêm một lần: đã có khách SĐT 0900000001 thì bỏ qua.
-- Xoá dữ liệu mẫu: xem đoạn cuối file.
-- =====================================================================

USE SpaThuCung;
GO

IF EXISTS (SELECT 1 FROM dbo.KhachHang WHERE sdt = '0900000001')
BEGIN
    PRINT N'Đã có dữ liệu mẫu, bỏ qua.';
    RETURN;
END

SET NOCOUNT ON;

-- Groomer mẫu (mật khẩu 123456, cùng mã băm với tài khoản nhanvien)
IF NOT EXISTS (SELECT 1 FROM dbo.NhanVien WHERE ten_dang_nhap = 'groomer1')
    INSERT INTO dbo.NhanVien (ho_ten, vai_tro, trang_thai_lam_viec, ten_dang_nhap, mat_khau)
    VALUES (N'Lê Thị Groomer', N'Nhân viên', N'Đang làm việc', 'groomer1',
            '$2a$11$VxHaGW1bH2tuOpIsmNnwFeIiZVQPcmqfm53FE185O6IvKd0sDyWsq');
IF NOT EXISTS (SELECT 1 FROM dbo.NhanVien WHERE ten_dang_nhap = 'groomer2')
    INSERT INTO dbo.NhanVien (ho_ten, vai_tro, trang_thai_lam_viec, ten_dang_nhap, mat_khau)
    VALUES (N'Phạm Văn Tỉa', N'Nhân viên', N'Đang làm việc', 'groomer2',
            '$2a$11$VxHaGW1bH2tuOpIsmNnwFeIiZVQPcmqfm53FE185O6IvKd0sDyWsq');

DECLARE @g1 INT = (SELECT ma_nv FROM dbo.NhanVien WHERE ten_dang_nhap = 'groomer1');
DECLARE @g2 INT = (SELECT ma_nv FROM dbo.NhanVien WHERE ten_dang_nhap = 'groomer2');
DECLARE @lap INT = (SELECT TOP 1 ma_nv FROM dbo.NhanVien WHERE ten_dang_nhap IN ('nhanvien', 'quanly') ORDER BY ma_nv DESC);

INSERT INTO dbo.KhachHang (ho_ten, sdt) VALUES
    (N'Trần Minh Anh', '0900000001'),
    (N'Võ Thị Bích', '0900000002'),
    (N'Đỗ Quốc Cường', '0900000003');

DECLARE @kh1 INT = (SELECT ma_kh FROM dbo.KhachHang WHERE sdt = '0900000001');
DECLARE @kh2 INT = (SELECT ma_kh FROM dbo.KhachHang WHERE sdt = '0900000002');
DECLARE @kh3 INT = (SELECT ma_kh FROM dbo.KhachHang WHERE sdt = '0900000003');

INSERT INTO dbo.ThuCung (ma_kh, ten, loai, can_nang, da_tiem_phong) VALUES
    (@kh1, N'Mochi', N'Chó', 4.5, 1),
    (@kh1, N'Bơ', N'Mèo', 3.2, 1),
    (@kh2, N'Lucky', N'Chó', 12, 1),
    (@kh3, N'Bông', N'Chó', 22, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Long)
    INSERT INTO dbo.Long (ten_long, loai_long) VALUES
        (N'L01', N'Đặt lịch'), (N'L02', N'Đặt lịch'), (N'W01', N'Walk-in'), (N'W02', N'Walk-in');

-- Thú cưng mẫu kèm số thứ tự để chọn xoay vòng.
DECLARE @tc TABLE (stt INT IDENTITY(0,1), ma INT, loai NVARCHAR(10), can DECIMAL(5,2));
INSERT INTO @tc (ma, loai, can)
SELECT t.ma_thu_cung, t.loai, t.can_nang
FROM dbo.ThuCung t JOIN dbo.KhachHang k ON k.ma_kh = t.ma_kh
WHERE k.sdt LIKE '090000000_'
ORDER BY t.ma_thu_cung;

DECLARE @ngay INT = -75, @i INT, @soPhieu INT, @maPhieu INT, @tcStt INT;
DECLARE @maTc INT, @loai NVARCHAR(10), @can DECIMAL(5,2), @hen DATETIME, @trangThai NVARCHAR(30), @tt NVARCHAR(20);
DECLARE @homNay DATE = CAST(GETDATE() AS DATE), @r INT;

WHILE @ngay <= 5
BEGIN
    SET @soPhieu = 1 + ABS(CHECKSUM(NEWID())) % 4;
    SET @i = 0;
    WHILE @i < @soPhieu
    BEGIN
        SET @tcStt = ABS(CHECKSUM(NEWID())) % 4;
        SELECT @maTc = ma, @loai = loai, @can = can FROM @tc WHERE stt = @tcStt;
        SET @hen = DATEADD(MINUTE, 8 * 60 + 90 * @i + 30 * (ABS(CHECKSUM(NEWID())) % 3), CAST(DATEADD(DAY, @ngay, @homNay) AS DATETIME));

        -- Ngày đã qua: phần lớn hoàn tất và đã thanh toán, có ít phiếu huỷ / không đến. Ngày tới: đã đặt lịch.
        SET @r = ABS(CHECKSUM(NEWID())) % 20;
        IF @ngay > 0
            SELECT @trangThai = N'Đã đặt lịch', @tt = N'Chưa thanh toán';
        ELSE IF @r = 0
            SELECT @trangThai = N'Đã huỷ', @tt = N'Chưa thanh toán';
        ELSE IF @r = 1
            SELECT @trangThai = N'Không đến', @tt = N'Chưa thanh toán';
        ELSE IF @ngay = 0 AND @r < 8
            SELECT @trangThai = N'Đã nhận thú cưng', @tt = N'Chưa thanh toán';
        ELSE
            SELECT @trangThai = N'Hoàn tất chăm sóc', @tt = N'Đã thanh toán';

        INSERT INTO dbo.PhieuDichVu (ma_thu_cung, ma_groomer, ma_nv_lap, nguon_tiep_nhan, ngay_gio_hen, trang_thai,
                                     can_nang_luc_lap, tt_thanh_toan, phuong_thuc, ly_do_huy, gio_nhan, gio_bat_dau, gio_tra, ngay_tao)
        VALUES (@maTc,
                CASE WHEN @trangThai IN (N'Đã huỷ', N'Không đến', N'Đã đặt lịch') THEN NULL
                     WHEN @i % 2 = 0 THEN @g1 ELSE @g2 END,
                @lap,
                CASE WHEN @i % 3 = 2 THEN N'Walk-in' ELSE N'Đặt lịch' END,
                @hen, @trangThai, @can, @tt,
                CASE WHEN @tt = N'Đã thanh toán' THEN CASE WHEN @r % 2 = 0 THEN N'Tiền mặt' ELSE N'Chuyển khoản' END END,
                CASE WHEN @trangThai = N'Đã huỷ' THEN N'Khách bận (dữ liệu mẫu)' END,
                CASE WHEN @trangThai IN (N'Hoàn tất chăm sóc', N'Đã nhận thú cưng') THEN @hen END,
                CASE WHEN @trangThai = N'Hoàn tất chăm sóc' THEN DATEADD(MINUTE, 10, @hen) END,
                CASE WHEN @trangThai = N'Hoàn tất chăm sóc' THEN DATEADD(MINUTE, 150, @hen) END,
                DATEADD(DAY, -1, @hen));
        SET @maPhieu = SCOPE_IDENTITY();

        -- Dịch vụ: luôn có Tắm sấy; thêm Cắt tỉa lông hoặc Vệ sinh tai, cắt móng tuỳ phiếu. Giá theo bảng giá (từ, đến].
        INSERT INTO dbo.ChiTietPhieu (ma_phieu, ma_dv, don_gia, thoi_luong)
        SELECT @maPhieu, g.ma_dv, g.gia, g.thoi_luong
        FROM dbo.BangGia g
        JOIN dbo.DichVu d ON d.ma_dv = g.ma_dv
        WHERE g.loai = @loai AND @can > g.can_nang_tu AND @can <= g.can_nang_den
          AND (d.ten_dv = N'Tắm sấy'
               OR (d.ten_dv = N'Cắt tỉa lông' AND (@maPhieu % 3) = 0)
               OR (d.ten_dv = N'Vệ sinh tai, cắt móng' AND (@maPhieu % 2) = 0));

        UPDATE p SET tong_tien = ISNULL(c.tien, 0), tong_thoi_gian = ISNULL(c.phut, 0)
        FROM dbo.PhieuDichVu p
        OUTER APPLY (SELECT SUM(don_gia) AS tien, SUM(thoi_luong) AS phut FROM dbo.ChiTietPhieu WHERE ma_phieu = p.ma_phieu) c
        WHERE p.ma_phieu = @maPhieu;

        SET @i += 1;
    END
    SET @ngay += 1;
END

PRINT N'Đã thêm dữ liệu mẫu cho Thống kê doanh thu.';
GO

/* ---------------------------------------------------------------------
   Xoá dữ liệu mẫu (bôi đen đoạn dưới rồi chạy):

USE SpaThuCung;
DELETE n FROM dbo.NhatKyPhieu n JOIN dbo.PhieuDichVu p ON p.ma_phieu = n.ma_phieu
    JOIN dbo.ThuCung t ON t.ma_thu_cung = p.ma_thu_cung JOIN dbo.KhachHang k ON k.ma_kh = t.ma_kh
    WHERE k.sdt LIKE '090000000_';
DELETE h FROM dbo.HinhAnhPhieu h JOIN dbo.PhieuDichVu p ON p.ma_phieu = h.ma_phieu
    JOIN dbo.ThuCung t ON t.ma_thu_cung = p.ma_thu_cung JOIN dbo.KhachHang k ON k.ma_kh = t.ma_kh
    WHERE k.sdt LIKE '090000000_';
DELETE c FROM dbo.ChiTietPhieu c JOIN dbo.PhieuDichVu p ON p.ma_phieu = c.ma_phieu
    JOIN dbo.ThuCung t ON t.ma_thu_cung = p.ma_thu_cung JOIN dbo.KhachHang k ON k.ma_kh = t.ma_kh
    WHERE k.sdt LIKE '090000000_';
DELETE p FROM dbo.PhieuDichVu p JOIN dbo.ThuCung t ON t.ma_thu_cung = p.ma_thu_cung
    JOIN dbo.KhachHang k ON k.ma_kh = t.ma_kh WHERE k.sdt LIKE '090000000_';
DELETE t FROM dbo.ThuCung t JOIN dbo.KhachHang k ON k.ma_kh = t.ma_kh WHERE k.sdt LIKE '090000000_';
DELETE FROM dbo.KhachHang WHERE sdt LIKE '090000000_';
--------------------------------------------------------------------- */

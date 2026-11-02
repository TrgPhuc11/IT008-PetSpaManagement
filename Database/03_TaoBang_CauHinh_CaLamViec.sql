-- =====================================================================
-- Spa thú cưng - Bảng CauHinh và CaLamViec (chức năng Cấu hình hệ thống, FR14)
-- Chạy sau 02_TaoBang_DichVu_BangGia.sql. Có thể chạy nhiều lần (idempotent).
-- Thông số mặc định được thêm theo từng mã nếu chưa có, không đè giá trị Quản lý đã chỉnh.
-- Hai ca mặc định chỉ thêm khi bảng CaLamViec còn trống.
-- =====================================================================

USE SpaThuCung;
GO

IF OBJECT_ID(N'dbo.CauHinh', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CauHinh
    (
        ma_cau_hinh  VARCHAR(50)    NOT NULL,
        gia_tri      NVARCHAR(100)  NOT NULL,
        mo_ta        NVARCHAR(255)  NULL,
        CONSTRAINT PK_CauHinh PRIMARY KEY (ma_cau_hinh)
    );
END
GO

-- Việc các ca đang sử dụng không chồng giờ nhau do ứng dụng kiểm tra (BR09).
IF OBJECT_ID(N'dbo.CaLamViec', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CaLamViec
    (
        ma_ca               INT IDENTITY(1,1) NOT NULL,
        ten_ca              NVARCHAR(30)      NOT NULL,
        gio_bat_dau         TIME(0)           NOT NULL,
        gio_ket_thuc        TIME(0)           NOT NULL,
        phut_vuot_cho_phep  INT               NOT NULL CONSTRAINT DF_CaLamViec_phut_vuot DEFAULT 30,
        so_le_tan           INT               NOT NULL CONSTRAINT DF_CaLamViec_so_le_tan DEFAULT 1,
        so_groomer          INT               NOT NULL CONSTRAINT DF_CaLamViec_so_groomer DEFAULT 2,
        dang_su_dung        BIT               NOT NULL CONSTRAINT DF_CaLamViec_dang_su_dung DEFAULT 1,
        CONSTRAINT PK_CaLamViec PRIMARY KEY (ma_ca),
        CONSTRAINT UQ_CaLamViec_ten_ca UNIQUE (ten_ca),
        CONSTRAINT CK_CaLamViec_gio CHECK (gio_ket_thuc > gio_bat_dau),
        CONSTRAINT CK_CaLamViec_phut_vuot CHECK (phut_vuot_cho_phep >= 0),
        CONSTRAINT CK_CaLamViec_so_nguoi CHECK (so_le_tan >= 0 AND so_groomer >= 0)
    );
END
GO

-- Thông số mặc định (thêm mã nào còn thiếu)
INSERT INTO dbo.CauHinh (ma_cau_hinh, gia_tri, mo_ta)
SELECT v.ma, v.gia_tri, v.mo_ta
FROM (VALUES
    ('THOI_GIAN_CHO_KHONG_DEN',   N'30',      N'Quá giờ hẹn bao nhiêu phút mà chưa nhận thú cưng thì tự chuyển Không đến'),
    ('NGUONG_CANH_BAO_KHONG_DEN', N'3',       N'Khách có từ bấy nhiêu lần Không đến thì cảnh báo khi đặt lịch'),
    ('THOI_HAN_DOI_LICH',         N'12',      N'Dời lịch phải trước giờ hẹn ít nhất bấy nhiêu giờ (báo trễ và huỷ không áp dụng)'),
    ('SO_ANH_TOI_DA',             N'5',       N'Số ảnh tối đa mỗi loại (Lúc nhận, Sau chăm sóc) của một phiếu'),
    ('THU_MUC_ANH',               N'HinhAnh', N'Thư mục lưu ảnh thú cưng; đường dẫn tương đối tính từ thư mục chương trình')
) AS v (ma, gia_tri, mo_ta)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CauHinh c WHERE c.ma_cau_hinh = v.ma);
GO

-- Ca làm việc mặc định
IF NOT EXISTS (SELECT 1 FROM dbo.CaLamViec)
BEGIN
    INSERT INTO dbo.CaLamViec (ten_ca, gio_bat_dau, gio_ket_thuc, phut_vuot_cho_phep, so_le_tan, so_groomer, dang_su_dung) VALUES
        (N'Ca sáng',      '08:00', '14:30', 30, 1, 2, 1),
        (N'Ca chiều/tối', '14:30', '21:00', 30, 1, 2, 1);
END
GO

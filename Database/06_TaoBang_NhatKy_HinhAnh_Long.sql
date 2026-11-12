-- =====================================================================
-- Spa thú cưng - Bảng NhatKyPhieu, HinhAnhPhieu và danh sách lồng mặc định
-- Theo Chương IV của báo cáo (mục 2.7, 2.10, 2.12 và mục 3 các giá trị CHECK).
-- Lập phiếu (UC10) ghi NhatKyPhieu khi nhận Walk-in hoặc nhập tay giá;
-- ảnh thú cưng (UC21) làm sau sẽ ghi vào HinhAnhPhieu.
-- Chạy sau 05_TaoBang_PhieuDichVu.sql. Có thể chạy nhiều lần (idempotent).
-- =====================================================================

USE SpaThuCung;
GO

IF OBJECT_ID(N'dbo.NhatKyPhieu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NhatKyPhieu
    (
        ma_log     INT IDENTITY(1,1) NOT NULL,
        ma_phieu   INT               NOT NULL,
        ma_nv      INT               NULL,
        thoi_diem  DATETIME          NOT NULL CONSTRAINT DF_NhatKyPhieu_thoi_diem DEFAULT GETDATE(),
        hanh_dong  NVARCHAR(30)      NOT NULL,
        noi_dung   NVARCHAR(500)     NOT NULL,
        CONSTRAINT PK_NhatKyPhieu PRIMARY KEY (ma_log),
        CONSTRAINT FK_NhatKyPhieu_PhieuDichVu FOREIGN KEY (ma_phieu) REFERENCES dbo.PhieuDichVu (ma_phieu),
        CONSTRAINT FK_NhatKyPhieu_NhanVien FOREIGN KEY (ma_nv) REFERENCES dbo.NhanVien (ma_nv),
        CONSTRAINT CK_NhatKyPhieu_hanh_dong CHECK (hanh_dong IN (N'Đổi dịch vụ', N'Đổi giờ hẹn',
            N'Dời do khách báo trễ', N'Đổi Groomer', N'Sửa cân nặng', N'Nhập tay giá', N'Nhận thú cưng',
            N'Đổi lồng', N'Trả thú cưng', N'Không đến', N'Thêm ảnh', N'Xoá ảnh'))
    );

    CREATE INDEX IX_NhatKyPhieu_ma_phieu ON dbo.NhatKyPhieu (ma_phieu);
END
GO

IF OBJECT_ID(N'dbo.HinhAnhPhieu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HinhAnhPhieu
    (
        ma_anh     INT IDENTITY(1,1) NOT NULL,
        ma_phieu   INT               NOT NULL,
        loai_anh   NVARCHAR(20)      NOT NULL,
        duong_dan  NVARCHAR(260)     NOT NULL,
        ghi_chu    NVARCHAR(255)     NULL,
        thoi_diem  DATETIME          NOT NULL CONSTRAINT DF_HinhAnhPhieu_thoi_diem DEFAULT GETDATE(),
        CONSTRAINT PK_HinhAnhPhieu PRIMARY KEY (ma_anh),
        CONSTRAINT UQ_HinhAnhPhieu_duong_dan UNIQUE (duong_dan),
        CONSTRAINT FK_HinhAnhPhieu_PhieuDichVu FOREIGN KEY (ma_phieu) REFERENCES dbo.PhieuDichVu (ma_phieu),
        CONSTRAINT CK_HinhAnhPhieu_loai_anh CHECK (loai_anh IN (N'Lúc nhận', N'Sau chăm sóc'))
    );

    CREATE INDEX IX_HinhAnhPhieu_ma_phieu ON dbo.HinhAnhPhieu (ma_phieu);
END
GO

-- Danh sách lồng của tiệm được khởi tạo sẵn (không có chức năng quản lý lồng).
-- Chỉ thêm khi bảng còn trống: 6 lồng Đặt lịch (L01–L06) và 4 lồng Walk-in (W01–W04).
-- Muốn đổi số lồng thì sửa bảng Long trong SSMS.
IF NOT EXISTS (SELECT 1 FROM dbo.Long)
BEGIN
    INSERT INTO dbo.Long (ten_long, loai_long) VALUES
        (N'L01', N'Đặt lịch'), (N'L02', N'Đặt lịch'), (N'L03', N'Đặt lịch'),
        (N'L04', N'Đặt lịch'), (N'L05', N'Đặt lịch'), (N'L06', N'Đặt lịch'),
        (N'W01', N'Walk-in'),  (N'W02', N'Walk-in'),  (N'W03', N'Walk-in'),  (N'W04', N'Walk-in');
END
GO

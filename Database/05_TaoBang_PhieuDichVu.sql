-- =====================================================================
-- Spa thú cưng - Bảng KhachHang, ThuCung, Long, PhieuDichVu, ChiTietPhieu
-- Theo Chương IV của báo cáo (mục 2.3, 2.4, 2.7, 2.8, 2.9).
-- Thống kê doanh thu (UC18, FR12) đọc PhieuDichVu và ChiTietPhieu; các màn
-- Khách hàng, Lập phiếu, Thanh toán làm sau sẽ ghi vào các bảng này.
-- Chạy sau 04_TaoBang_LichLamViec.sql. Có thể chạy nhiều lần (idempotent).
-- =====================================================================

USE SpaThuCung;
GO

IF OBJECT_ID(N'dbo.KhachHang', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KhachHang
    (
        ma_kh    INT IDENTITY(1,1) NOT NULL,
        ho_ten   NVARCHAR(100)     NOT NULL,
        sdt      VARCHAR(15)       NOT NULL,
        ghi_chu  NVARCHAR(255)     NULL,
        CONSTRAINT PK_KhachHang PRIMARY KEY (ma_kh),
        CONSTRAINT UQ_KhachHang_sdt UNIQUE (sdt)
    );
END
GO

IF OBJECT_ID(N'dbo.ThuCung', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ThuCung
    (
        ma_thu_cung    INT IDENTITY(1,1) NOT NULL,
        ma_kh          INT               NOT NULL,
        ten            NVARCHAR(50)      NOT NULL,
        loai           NVARCHAR(10)      NOT NULL,
        can_nang       DECIMAL(5,2)      NOT NULL,
        da_tiem_phong  BIT               NOT NULL CONSTRAINT DF_ThuCung_da_tiem_phong DEFAULT 0,
        ghi_chu        NVARCHAR(255)     NULL,
        CONSTRAINT PK_ThuCung PRIMARY KEY (ma_thu_cung),
        CONSTRAINT FK_ThuCung_KhachHang FOREIGN KEY (ma_kh) REFERENCES dbo.KhachHang (ma_kh),
        CONSTRAINT CK_ThuCung_loai CHECK (loai IN (N'Chó', N'Mèo')),
        CONSTRAINT CK_ThuCung_can_nang CHECK (can_nang > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Long', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Long
    (
        ma_long    INT IDENTITY(1,1) NOT NULL,
        ten_long   NVARCHAR(20)      NOT NULL,
        loai_long  NVARCHAR(20)      NOT NULL,
        ghi_chu    NVARCHAR(255)     NULL,
        CONSTRAINT PK_Long PRIMARY KEY (ma_long),
        CONSTRAINT UQ_Long_ten_long UNIQUE (ten_long),
        CONSTRAINT CK_Long_loai_long CHECK (loai_long IN (N'Đặt lịch', N'Walk-in'))
    );
END
GO

IF OBJECT_ID(N'dbo.PhieuDichVu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhieuDichVu
    (
        ma_phieu          INT IDENTITY(1,1) NOT NULL,
        ma_thu_cung       INT               NOT NULL,
        ma_groomer        INT               NULL,
        ma_nv_lap         INT               NOT NULL,
        ma_long           INT               NULL,
        nguon_tiep_nhan   NVARCHAR(20)      NOT NULL,
        ngay_gio_hen      DATETIME          NOT NULL,
        trang_thai        NVARCHAR(30)      NOT NULL,
        can_nang_luc_lap  DECIMAL(5,2)      NOT NULL,
        tong_tien         DECIMAL(12,0)     NOT NULL CONSTRAINT DF_PhieuDichVu_tong_tien DEFAULT 0,
        tong_thoi_gian    INT               NOT NULL CONSTRAINT DF_PhieuDichVu_tong_thoi_gian DEFAULT 0,
        tt_thanh_toan     NVARCHAR(20)      NOT NULL CONSTRAINT DF_PhieuDichVu_tt_thanh_toan DEFAULT N'Chưa thanh toán',
        phuong_thuc       NVARCHAR(20)      NULL,
        ly_do_huy         NVARCHAR(255)     NULL,
        gio_nhan          DATETIME          NULL,
        gio_bat_dau       DATETIME          NULL,
        gio_tra           DATETIME          NULL,
        ngay_tao          DATETIME          NOT NULL CONSTRAINT DF_PhieuDichVu_ngay_tao DEFAULT GETDATE(),
        CONSTRAINT PK_PhieuDichVu PRIMARY KEY (ma_phieu),
        CONSTRAINT FK_PhieuDichVu_ThuCung FOREIGN KEY (ma_thu_cung) REFERENCES dbo.ThuCung (ma_thu_cung),
        CONSTRAINT FK_PhieuDichVu_Groomer FOREIGN KEY (ma_groomer) REFERENCES dbo.NhanVien (ma_nv),
        CONSTRAINT FK_PhieuDichVu_NvLap FOREIGN KEY (ma_nv_lap) REFERENCES dbo.NhanVien (ma_nv),
        CONSTRAINT FK_PhieuDichVu_Long FOREIGN KEY (ma_long) REFERENCES dbo.Long (ma_long),
        CONSTRAINT CK_PhieuDichVu_nguon CHECK (nguon_tiep_nhan IN (N'Đặt lịch', N'Walk-in')),
        CONSTRAINT CK_PhieuDichVu_trang_thai CHECK (trang_thai IN (N'Đã đặt lịch', N'Đã nhận thú cưng',
            N'Đang thực hiện chăm sóc', N'Hoàn tất chăm sóc', N'Đã huỷ', N'Không đến')),
        CONSTRAINT CK_PhieuDichVu_tong_tien CHECK (tong_tien >= 0),
        CONSTRAINT CK_PhieuDichVu_tong_thoi_gian CHECK (tong_thoi_gian >= 0),
        CONSTRAINT CK_PhieuDichVu_tt_thanh_toan CHECK (tt_thanh_toan IN (N'Chưa thanh toán', N'Đã thanh toán')),
        CONSTRAINT CK_PhieuDichVu_phuong_thuc CHECK (phuong_thuc IS NULL OR phuong_thuc IN (N'Tiền mặt', N'Chuyển khoản'))
    );

    -- Thống kê doanh thu và danh sách phiếu đều lọc theo ngày hẹn.
    CREATE INDEX IX_PhieuDichVu_ngay_gio_hen ON dbo.PhieuDichVu (ngay_gio_hen) INCLUDE (trang_thai, tt_thanh_toan, tong_tien, ma_groomer);
END
GO

IF OBJECT_ID(N'dbo.ChiTietPhieu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChiTietPhieu
    (
        ma_phieu    INT            NOT NULL,
        ma_dv       INT            NOT NULL,
        don_gia     DECIMAL(12,0)  NOT NULL,
        thoi_luong  INT            NOT NULL,
        ghi_chu     NVARCHAR(255)  NULL,
        CONSTRAINT PK_ChiTietPhieu PRIMARY KEY (ma_phieu, ma_dv),
        CONSTRAINT FK_ChiTietPhieu_PhieuDichVu FOREIGN KEY (ma_phieu) REFERENCES dbo.PhieuDichVu (ma_phieu),
        CONSTRAINT FK_ChiTietPhieu_DichVu FOREIGN KEY (ma_dv) REFERENCES dbo.DichVu (ma_dv),
        CONSTRAINT CK_ChiTietPhieu_don_gia CHECK (don_gia >= 0),
        CONSTRAINT CK_ChiTietPhieu_thoi_luong CHECK (thoi_luong > 0)
    );

    CREATE INDEX IX_ChiTietPhieu_ma_dv ON dbo.ChiTietPhieu (ma_dv);
END
GO

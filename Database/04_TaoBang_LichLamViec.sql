-- =====================================================================
-- Spa thú cưng - Bảng LichLamViec (chức năng Xếp lịch làm việc, FR13, UC05)
-- Chạy sau 03_TaoBang_CauHinh_CaLamViec.sql. Có thể chạy nhiều lần (idempotent).
-- Mỗi nhân viên tối đa một ca, một vị trí trong một ngày (BR02).
-- =====================================================================

USE SpaThuCung;
GO

IF OBJECT_ID(N'dbo.LichLamViec', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichLamViec
    (
        ma_lich  INT IDENTITY(1,1) NOT NULL,
        ma_nv    INT               NOT NULL,
        ngay     DATE              NOT NULL,
        ma_ca    INT               NOT NULL,
        vi_tri   NVARCHAR(20)      NOT NULL,
        CONSTRAINT PK_LichLamViec PRIMARY KEY (ma_lich),
        CONSTRAINT FK_LichLamViec_NhanVien FOREIGN KEY (ma_nv) REFERENCES dbo.NhanVien (ma_nv),
        CONSTRAINT FK_LichLamViec_CaLamViec FOREIGN KEY (ma_ca) REFERENCES dbo.CaLamViec (ma_ca),
        CONSTRAINT UQ_LichLamViec_nv_ngay UNIQUE (ma_nv, ngay),
        CONSTRAINT CK_LichLamViec_vi_tri CHECK (vi_tri IN (N'Lễ tân', N'Groomer'))
    );

    -- Lấy lịch theo tuần và danh sách Groomer trong một ca của một ngày.
    CREATE INDEX IX_LichLamViec_ngay_ca ON dbo.LichLamViec (ngay, ma_ca);
END
GO

-- =====================================================================
-- Spa thú cưng - Bảng DichVu và BangGia (chức năng Dịch vụ và bảng giá)
-- Chạy sau 01_TaoCSDL_NhanVien.sql. Có thể chạy nhiều lần (idempotent).
-- Dữ liệu mẫu chỉ thêm khi bảng DichVu còn trống.
-- =====================================================================

USE SpaThuCung;
GO

IF OBJECT_ID(N'dbo.DichVu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DichVu
    (
        ma_dv       INT IDENTITY(1,1) NOT NULL,
        ten_dv      NVARCHAR(100)     NOT NULL,
        mo_ta       NVARCHAR(255)     NULL,
        trang_thai  NVARCHAR(20)      NOT NULL CONSTRAINT DF_DichVu_trang_thai DEFAULT N'Đang cung cấp',
        CONSTRAINT PK_DichVu PRIMARY KEY (ma_dv),
        CONSTRAINT UQ_DichVu_ten_dv UNIQUE (ten_dv),
        CONSTRAINT CK_DichVu_trang_thai CHECK (trang_thai IN (N'Đang cung cấp', N'Ngừng cung cấp'))
    );
END
GO

-- Khoảng cân nặng tính từ trên can_nang_tu đến hết can_nang_den.
-- Việc các khoảng của cùng dịch vụ và loài không chồng lấn do ứng dụng kiểm tra (BR07).
IF OBJECT_ID(N'dbo.BangGia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BangGia
    (
        ma_gia        INT IDENTITY(1,1) NOT NULL,
        ma_dv         INT               NOT NULL,
        loai          NVARCHAR(10)      NOT NULL,
        can_nang_tu   DECIMAL(5,2)      NOT NULL,
        can_nang_den  DECIMAL(5,2)      NOT NULL,
        gia           DECIMAL(12,0)     NOT NULL,
        thoi_luong    INT               NOT NULL,
        CONSTRAINT PK_BangGia PRIMARY KEY (ma_gia),
        CONSTRAINT FK_BangGia_DichVu FOREIGN KEY (ma_dv) REFERENCES dbo.DichVu (ma_dv),
        CONSTRAINT CK_BangGia_loai CHECK (loai IN (N'Chó', N'Mèo')),
        CONSTRAINT CK_BangGia_can_nang CHECK (can_nang_tu >= 0 AND can_nang_den > can_nang_tu),
        CONSTRAINT CK_BangGia_gia CHECK (gia >= 0),
        CONSTRAINT CK_BangGia_thoi_luong CHECK (thoi_luong > 0)
    );
END
GO

-- Dữ liệu mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.DichVu)
BEGIN
    DECLARE @tam INT, @cat INT, @tai INT, @spa INT;

    INSERT INTO dbo.DichVu (ten_dv, mo_ta, trang_thai) VALUES (N'Tắm sấy', N'Tắm, sấy khô, chải lông', N'Đang cung cấp');
    SET @tam = SCOPE_IDENTITY();
    INSERT INTO dbo.DichVu (ten_dv, mo_ta, trang_thai) VALUES (N'Cắt tỉa lông', N'Cắt tỉa tạo kiểu theo yêu cầu', N'Đang cung cấp');
    SET @cat = SCOPE_IDENTITY();
    INSERT INTO dbo.DichVu (ten_dv, mo_ta, trang_thai) VALUES (N'Vệ sinh tai, cắt móng', N'Làm sạch tai, cắt và mài móng', N'Đang cung cấp');
    SET @tai = SCOPE_IDENTITY();
    INSERT INTO dbo.DichVu (ten_dv, mo_ta, trang_thai) VALUES (N'Spa thảo dược', N'Tắm thảo dược, massage', N'Ngừng cung cấp');
    SET @spa = SCOPE_IDENTITY();

    INSERT INTO dbo.BangGia (ma_dv, loai, can_nang_tu, can_nang_den, gia, thoi_luong) VALUES
        (@tam, N'Chó', 0, 5, 150000, 45),
        (@tam, N'Chó', 5, 15, 220000, 60),
        (@tam, N'Chó', 15, 30, 320000, 90),
        (@tam, N'Mèo', 0, 5, 180000, 50),
        (@tam, N'Mèo', 5, 10, 240000, 70),
        (@cat, N'Chó', 0, 5, 250000, 60),
        (@cat, N'Chó', 5, 15, 350000, 90),
        (@cat, N'Chó', 15, 30, 480000, 120),
        (@cat, N'Mèo', 0, 10, 300000, 80),
        (@tai, N'Chó', 0, 30, 80000, 20),
        (@tai, N'Mèo', 0, 10, 80000, 20),
        (@spa, N'Chó', 0, 30, 200000, 40);
END
GO

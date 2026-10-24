-- =====================================================================
-- Spa thú cưng - Tạo CSDL và bảng NhanVien (phục vụ chức năng Đăng nhập)
-- Có thể chạy nhiều lần: chỉ tạo khi chưa có (idempotent).
-- Chạy tay trong SSMS, hoặc để ứng dụng tự chạy khi khởi động.
-- Mật khẩu 3 tài khoản mẫu đều là 123456 (đã băm BCrypt).
-- =====================================================================

IF DB_ID('SpaThuCung') IS NULL
    CREATE DATABASE SpaThuCung;
GO

USE SpaThuCung;
GO

IF OBJECT_ID(N'dbo.NhanVien', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NhanVien
    (
        ma_nv                INT IDENTITY(1,1) NOT NULL,
        ho_ten               NVARCHAR(100)     NOT NULL,
        sdt                  VARCHAR(15)       NULL,
        vai_tro              NVARCHAR(20)      NOT NULL,
        trang_thai_lam_viec  NVARCHAR(20)      NOT NULL CONSTRAINT DF_NhanVien_trang_thai DEFAULT N'Đang làm việc',
        ten_dang_nhap        VARCHAR(50)       NOT NULL,
        mat_khau             VARCHAR(255)      NOT NULL,
        CONSTRAINT PK_NhanVien PRIMARY KEY (ma_nv),
        CONSTRAINT UQ_NhanVien_ten_dang_nhap UNIQUE (ten_dang_nhap),
        CONSTRAINT CK_NhanVien_vai_tro CHECK (vai_tro IN (N'Nhân viên', N'Quản lý')),
        CONSTRAINT CK_NhanVien_trang_thai CHECK (trang_thai_lam_viec IN (N'Đang làm việc', N'Đã nghỉ việc'))
    );
END
GO

-- Tài khoản mẫu (mật khẩu: 123456)
IF NOT EXISTS (SELECT 1 FROM dbo.NhanVien WHERE ten_dang_nhap = 'quanly')
    INSERT INTO dbo.NhanVien (ho_ten, vai_tro, trang_thai_lam_viec, ten_dang_nhap, mat_khau)
    VALUES (N'Nguyễn Văn Quản', N'Quản lý', N'Đang làm việc', 'quanly',
            '$2a$11$nMZX/qHStJLHcXJ5I1A.nOFPnC.VaxcKpVO8PoXBFYs0nA9hhTRoS');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.NhanVien WHERE ten_dang_nhap = 'nhanvien')
    INSERT INTO dbo.NhanVien (ho_ten, vai_tro, trang_thai_lam_viec, ten_dang_nhap, mat_khau)
    VALUES (N'Trần Thị Lễ Tân', N'Nhân viên', N'Đang làm việc', 'nhanvien',
            '$2a$11$VxHaGW1bH2tuOpIsmNnwFeIiZVQPcmqfm53FE185O6IvKd0sDyWsq');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.NhanVien WHERE ten_dang_nhap = 'nghiviec')
    INSERT INTO dbo.NhanVien (ho_ten, vai_tro, trang_thai_lam_viec, ten_dang_nhap, mat_khau)
    VALUES (N'Lê Văn Nghỉ', N'Nhân viên', N'Đã nghỉ việc', 'nghiviec',
            '$2a$11$m7c08IExqkWXhUPOd7mxZOoe3xkTn6m8aLuivOMXE7GuUIKlOI4Hi');
GO

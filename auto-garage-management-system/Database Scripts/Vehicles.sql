USE SmartGarage
GO


CREATE TABLE Vehicles (
    VehicleId       INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId      INT NOT NULL,
    LicensePlate    NVARCHAR(20) NOT NULL,
    Make            NVARCHAR(50) NOT NULL,
    Model           NVARCHAR(50) NOT NULL,
    Year            INT NULL,
    Color           NVARCHAR(30) NULL,
    VIN             NVARCHAR(50) NULL,
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Vehicles_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)
);
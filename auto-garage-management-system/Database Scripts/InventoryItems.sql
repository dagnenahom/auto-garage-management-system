use SmartGarage
go


CREATE TABLE InventoryItems (
    InventoryItemId INT IDENTITY(1,1) PRIMARY KEY,
    PartName        NVARCHAR(100) NOT NULL,
    PartNumber      NVARCHAR(50) NULL,
    Description     NVARCHAR(500) NULL,
    QuantityInStock INT NOT NULL DEFAULT 0,
    UnitPrice       DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    ReorderLevel    INT NOT NULL DEFAULT 5,
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETDATE()
);
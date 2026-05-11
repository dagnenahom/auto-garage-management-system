USE SmartGarage
GO
CREATE TABLE JobCardItems (
    JobCardItemId  INT IDENTITY(1,1) PRIMARY KEY,
    JobCardId      INT NOT NULL,
    InventoryItemId INT NOT NULL,
    Quantity       INT NOT NULL DEFAULT 1,
    UnitPrice      DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_JobCardItems_JobCards   FOREIGN KEY (JobCardId)       REFERENCES JobCards(JobCardId),
    CONSTRAINT FK_JobCardItems_Inventory FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(InventoryItemId)
);
USE SmartGarage
GO
CREATE TABLE JobCards (
    JobCardId      INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId      INT NOT NULL,
    AssignedUserId INT NOT NULL,
    Status         NVARCHAR(20) NOT NULL CHECK (Status IN ('Open','InProgress','Completed','Closed')),
    Description    NVARCHAR(500) NULL,
    DateCreated    DATETIME2 NOT NULL DEFAULT GETDATE(),
    DateCompleted  DATETIME2 NULL,
    CONSTRAINT FK_JobCards_Vehicles FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId),
    CONSTRAINT FK_JobCards_Users    FOREIGN KEY (AssignedUserId) REFERENCES Users(UserId)
);
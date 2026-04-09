IF DB_ID('VideoRentingSystemDb') IS NULL
BEGIN
    CREATE DATABASE VideoRentingSystemDb;
END;
GO

USE VideoRentingSystemDb;
GO

IF OBJECT_ID('dbo.Videos', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Videos;
END;
GO

CREATE TABLE dbo.Videos
(
    VideoId INT NOT NULL PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Genre NVARCHAR(100) NOT NULL,
    ReleaseYear INT NOT NULL,
    IsRented BIT NOT NULL DEFAULT 0,
    OwnerPublisherId INT NOT NULL DEFAULT 0,
    VideoType INT NOT NULL DEFAULT 0,
    RentalPrice DECIMAL(10,2) NOT NULL DEFAULT 2.99,
    RentalHours INT NOT NULL DEFAULT 48,
    IsPublished BIT NOT NULL DEFAULT 1
);
GO

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Users;
END;
GO

CREATE TABLE dbo.Users
(
    UserId INT NOT NULL PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Role INT NOT NULL DEFAULT 0,
    StudioName NVARCHAR(200) NULL
);
GO

IF OBJECT_ID('dbo.Rentals', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Rentals;
END;
GO

CREATE TABLE dbo.Rentals
(
    UserId INT NOT NULL,
    VideoId INT NOT NULL,
    RentDate DATETIME2 NOT NULL,
    ExpiryUtc DATETIME2 NOT NULL,
    PaidAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    CONSTRAINT PK_Rentals PRIMARY KEY (UserId, VideoId),
    CONSTRAINT FK_Rentals_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Rentals_Videos FOREIGN KEY (VideoId) REFERENCES dbo.Videos(VideoId)
);
GO

INSERT INTO dbo.Videos (VideoId, Title, Genre, ReleaseYear, IsRented, OwnerPublisherId, VideoType, RentalPrice, RentalHours, IsPublished)
VALUES
    (1001, N'Inception', N'Sci-Fi', 2010, 0, 0, 0, 2.99, 48, 1),
    (1002, N'Titanic', N'Drama', 1997, 0, 0, 0, 2.49, 48, 1),
    (1003, N'The Matrix', N'Sci-Fi', 1999, 1, 0, 0, 3.49, 72, 1),
    (1004, N'Interstellar', N'Sci-Fi', 2014, 0, 0, 0, 3.99, 72, 1),
    (1005, N'Toy Story', N'Animation', 1995, 0, 0, 0, 1.99, 48, 1);
GO

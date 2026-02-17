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
    IsRented BIT NOT NULL DEFAULT 0
);
GO

INSERT INTO dbo.Videos (VideoId, Title, Genre, ReleaseYear, IsRented)
VALUES
    (1001, N'Inception', N'Sci-Fi', 2010, 0),
    (1002, N'Titanic', N'Drama', 1997, 0),
    (1003, N'The Matrix', N'Sci-Fi', 1999, 1),
    (1004, N'Interstellar', N'Sci-Fi', 2014, 0),
    (1005, N'Toy Story', N'Animation', 1995, 0);
GO

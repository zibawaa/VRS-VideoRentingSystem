-- CST2550 sample data: T-SQL for SQL Server / LocalDB (matches the WinForms table layout).
-- Use: attach your .mdf in SSMS, select that database, then run this script to (re)seed rows.
-- The WinForms app also auto-creates dbo.VideoRentals on Connect if it is missing.

IF OBJECT_ID(N'dbo.VideoRentals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VideoRentals (
        RentalID INT NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Genre NVARCHAR(100) NOT NULL,
        Director NVARCHAR(200) NOT NULL,
        [Year] INT NOT NULL,
        RentalPrice DECIMAL(10,2) NOT NULL,
        AvailableCopies INT NOT NULL
    );
END
GO

DELETE FROM dbo.VideoRentals;
GO

INSERT INTO dbo.VideoRentals (RentalID, Title, Genre, Director, [Year], RentalPrice, AvailableCopies) VALUES
(1001, N'The Matrix', N'Sci-Fi', N'Wachowskis', 1999, 3.50, 4),
(1002, N'Inception', N'Sci-Fi', N'Christopher Nolan', 2010, 4.00, 2),
(1003, N'Parasite', N'Thriller', N'Bong Joon-ho', 2019, 3.75, 3),
(1004, N'Spirited Away', N'Animation', N'Hayao Miyazaki', 2001, 2.99, 5),
(1005, N'Mad Max Fury Road', N'Action', N'George Miller', 2015, 3.25, 2),
(1006, N'Whiplash', N'Drama', N'Damien Chazelle', 2014, 2.50, 3),
(1007, N'Get Out', N'Horror', N'Jordan Peele', 2017, 3.00, 4),
(1008, N'Paddington 2', N'Family', N'Paul King', 2017, 2.25, 6),
(1009, N'Arrival', N'Sci-Fi', N'Denis Villeneuve', 2016, 3.40, 2),
(1010, N'Knives Out', N'Mystery', N'Rian Johnson', 2019, 3.10, 3),
(1011, N'Everything Everywhere All At Once', N'Comedy', N'Daniels', 2022, 3.60, 2),
(1012, N'Blade Runner 2049', N'Sci-Fi', N'Denis Villeneuve', 2017, 3.55, 1);
GO

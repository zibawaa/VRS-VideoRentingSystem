CREATE TABLE IF NOT EXISTS Videos
(
    VideoId INTEGER NOT NULL PRIMARY KEY,
    Title TEXT NOT NULL,
    Genre TEXT NOT NULL,
    ReleaseYear INTEGER NOT NULL,
    IsRented INTEGER NOT NULL DEFAULT 0,
    OwnerPublisherId INTEGER NOT NULL DEFAULT 0,
    VideoType INTEGER NOT NULL DEFAULT 0,
    RentalPrice REAL NOT NULL DEFAULT 2.99,
    RentalHours INTEGER NOT NULL DEFAULT 48,
    IsPublished INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Users
(
    UserId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Role INTEGER NOT NULL DEFAULT 0,
    StudioName TEXT NULL
);

CREATE TABLE IF NOT EXISTS Rentals
(
    UserId INTEGER NOT NULL,
    VideoId INTEGER NOT NULL,
    RentDate TEXT NOT NULL,
    ExpiryUtc TEXT NOT NULL,
    PaidAmount REAL NOT NULL DEFAULT 0,
    PRIMARY KEY(UserId, VideoId),
    FOREIGN KEY(UserId) REFERENCES Users(UserId),
    FOREIGN KEY(VideoId) REFERENCES Videos(VideoId)
);

INSERT OR IGNORE INTO Videos (VideoId, Title, Genre, ReleaseYear, IsRented, OwnerPublisherId, VideoType, RentalPrice, RentalHours, IsPublished)
VALUES
    (1001, 'Inception', 'Sci-Fi', 2010, 0, 0, 0, 2.99, 48, 1),
    (1002, 'Titanic', 'Drama', 1997, 0, 0, 0, 2.49, 48, 1),
    (1003, 'The Matrix', 'Sci-Fi', 1999, 1, 0, 0, 3.49, 72, 1),
    (1004, 'Interstellar', 'Sci-Fi', 2014, 0, 0, 0, 3.99, 72, 1),
    (1005, 'Toy Story', 'Animation', 1995, 0, 0, 0, 1.99, 48, 1);

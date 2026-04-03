CREATE TABLE IF NOT EXISTS Videos
(
    VideoId INTEGER NOT NULL PRIMARY KEY,
    Title TEXT NOT NULL,
    Genre TEXT NOT NULL,
    ReleaseYear INTEGER NOT NULL,
    IsRented INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Users
(
    UserId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Rentals
(
    UserId INTEGER NOT NULL,
    VideoId INTEGER NOT NULL,
    RentDate TEXT NOT NULL,
    PRIMARY KEY(UserId, VideoId),
    FOREIGN KEY(UserId) REFERENCES Users(UserId),
    FOREIGN KEY(VideoId) REFERENCES Videos(VideoId)
);

INSERT OR IGNORE INTO Videos (VideoId, Title, Genre, ReleaseYear, IsRented)
VALUES
    (1001, 'Inception', 'Sci-Fi', 2010, 0),
    (1002, 'Titanic', 'Drama', 1997, 0),
    (1003, 'The Matrix', 'Sci-Fi', 1999, 1),
    (1004, 'Interstellar', 'Sci-Fi', 2014, 0),
    (1005, 'Toy Story', 'Animation', 1995, 0);

CREATE TABLE IF NOT EXISTS Videos
(
    VideoId INTEGER NOT NULL PRIMARY KEY,
    Title TEXT NOT NULL,
    Genre TEXT NOT NULL,
    ReleaseYear INTEGER NOT NULL,
    IsRented INTEGER NOT NULL DEFAULT 0
);

INSERT OR IGNORE INTO Videos (VideoId, Title, Genre, ReleaseYear, IsRented)
VALUES
    (1001, 'Inception', 'Sci-Fi', 2010, 0),
    (1002, 'Titanic', 'Drama', 1997, 0),
    (1003, 'The Matrix', 'Sci-Fi', 1999, 1),
    (1004, 'Interstellar', 'Sci-Fi', 2014, 0),
    (1005, 'Toy Story', 'Animation', 1995, 0);

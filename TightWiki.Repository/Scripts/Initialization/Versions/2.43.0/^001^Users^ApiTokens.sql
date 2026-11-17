--##IF TABLE NOT EXISTS(ApiTokens)

CREATE TABLE ApiTokens (
    Id          INTEGER NOT NULL,
    UserId      TEXT    NOT NULL,
    Name        TEXT    NOT NULL,
    TokenHash   TEXT    NOT NULL,
    CreatedDate TEXT    NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT)
);

CREATE UNIQUE INDEX IX_ApiTokens_TokenHash ON ApiTokens (TokenHash);

CREATE INDEX IX_ApiTokens_UserId ON ApiTokens (UserId);

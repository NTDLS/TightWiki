DELETE FROM CryptoCheck;
INSERT INTO CryptoCheck(Content) SELECT @Content

DELETE FROM EmojiCategory WHERE EmojiId = @Id;
DELETE FROM Emoji WHERE Id = @Id;

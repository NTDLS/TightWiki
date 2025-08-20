--##IF COLUMN NOT EXISTS(Role, IsBuiltIn)

ALTER TABLE [Role] ADD COLUMN [IsBuiltIn] INTEGER NOT NULL DEFAULT 1;

CREATE UNIQUE INDEX IF NOT EXISTS IX_Role_Name ON Role(Name);

--Reset seed data.
INSERT INTO Role (Name, Description, IsBuiltIn)
VALUES
  ('Administrator','Administrators can do anything. Add, edit, delete, etc.', 1),
  ('Member','Read-only user with a profile.', 1),
  ('Contributor','Contributor can add and edit unprotected pages.', 1),
  ('Moderator','Moderators can add, edit, and delete pages - including protected pages.', 1),
  ('Anonymous','Role applied to users who are not logged in.', 1)
ON CONFLICT(Name) DO UPDATE SET
  Description = excluded.Description,
  IsBuiltIn  = excluded.IsBuiltIn;

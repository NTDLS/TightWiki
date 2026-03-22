INSERT INTO Theme
(
	Name,
	DelimitedFiles,
	ClassNavBar,
	ClassNavLink,
	ClassDropdown,
	ClassBranding,
	EditorTheme
)
SELECT
	@Name,
	@DelimitedFiles,
	@ClassNavBar,
	@ClassNavLink,
	@ClassDropdown,
	@ClassBranding,
	@EditorTheme
ON CONFLICT(Name) DO UPDATE
SET
	DelimitedFiles = @DelimitedFiles,
	ClassNavBar = @ClassNavBar,
	ClassNavLink = @ClassNavLink,
	ClassDropdown = @ClassDropdown,
	ClassBranding = @ClassBranding,
	EditorTheme = @EditorTheme;

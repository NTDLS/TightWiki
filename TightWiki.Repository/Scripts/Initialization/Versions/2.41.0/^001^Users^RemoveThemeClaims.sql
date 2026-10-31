--##IF TABLE EXISTS(AspNetUserClaims)
--The per-user theme selection is no longer stored; it is now a browser-local preference.

DELETE FROM
	AspNetUserClaims
WHERE
	ClaimType = 'theme'

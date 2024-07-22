SELECT
	[Id] as [Id],
	[Name] as [Name],
	[Link] as [Link],
	[Ordinal] as [Ordinal]
FROM
	[MenuItem]
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Id=Id
Name=Name
Link=Link
Ordinal=Ordinal
*/
--::CONFIG
ORDER BY
	[Ordinal]
--::CUSTOM_ORDER_BEGIN

SELECT
	Id,
	[Name],
	[Description]
FROM
	[Role]
	[MenuItem]
--CUSTOM_ORDER_BY>>
--CONFIG>>
/*
Name=Name
Description=Description
*/
--<<CONFIG
ORDER BY
	[Name]
--<<CUSTOM_ORDER_BY

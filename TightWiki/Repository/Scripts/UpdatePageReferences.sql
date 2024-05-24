BEGIN TRANSACTION;

DELETE FROM
	PageReference
WHERE
	PageId = @PageId;

INSERT INTO PageReference
(
	PageId,
	ReferencesPageName,
	ReferencesPageNavigation,
	ReferencesPageId
)
SELECT DISTINCT
	@PageId,
	Coalesce(Ref.[Namespace] || ' :: ', '') ||  Ref.Name,
	Ref.Navigation,
	P.Id
FROM
	TempReferences as Ref
LEFT OUTER JOIN [Page] as P
	ON P.Navigation = Ref.Navigation;

UPDATE
	PageReference
SET
	ReferencesPageNavigation = I.Navigation
FROM (
		SELECT DISTINCT
			Id,
			P.Navigation
		FROM
			PageReference as PR
		INNER JOIN [Page] as P
			ON P.Id = PR.ReferencesPageId
		WHERE
			P.Id = 77
	) AS I
WHERE
	I.Id = PageReference.ReferencesPageId;

COMMIT TRANSACTION;

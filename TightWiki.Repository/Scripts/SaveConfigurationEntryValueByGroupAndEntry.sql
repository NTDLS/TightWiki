UPDATE
	ConfigurationEntry
SET
	Value = @Value
WHERE
	Name = @EntryName
	AND ConfigurationGroupId IN
		(
			SELECT
				CG.Id
			FROM
				ConfigurationGroup as CG
			WHERE
				CG.Name = @GroupName
		)

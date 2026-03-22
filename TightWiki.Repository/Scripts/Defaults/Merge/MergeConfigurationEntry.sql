INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT CG.Id FROM ConfigurationGroup as CG WHERE CG.Name = @ConfigurationGroupName LIMIT 1),
	@Name,
	@Value,
	@DataTypeId,
	@Description,
	@IsEncrypted,
	@IsRequired
ON CONFLICT(ConfigurationGroupId, Name)
DO UPDATE
SET
	Name = @Name,
	DataTypeId = @DataTypeId,
	Description = @Description,
	IsEncrypted = @IsEncrypted,
	IsRequired = @IsRequired

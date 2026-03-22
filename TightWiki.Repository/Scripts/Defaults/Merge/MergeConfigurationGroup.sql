INSERT INTO ConfigurationGroup(Name, Description)
SELECT @Name, @Description
ON CONFLICT(Name) DO UPDATE SET Description = @Description;

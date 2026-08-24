using Dapper;
using Microsoft.Extensions.Configuration;
using TightWiki.Plugin.Interfaces;
using TightWiki.Repository.Helpers;

// Usage: SchemaUpgradeTool <path-to-directory-containing-the-8-database-files>
//
// Applies the same versioned schema upgrade path that TightWiki\Program.cs runs at startup
// (DatabaseManager.InitializeSchema -> ApplyDatabaseUpgradeScripts) against the given directory.
// Intended to run against a *temporary copy* of Data\*.db, never against the real Data\ folder.

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: SchemaUpgradeTool <path-to-directory-containing-the-8-database-files>");
    return 1;
}

var databasePath = Path.GetFullPath(args[0]);

if (!Directory.Exists(databasePath))
{
    Console.Error.WriteLine($"Database directory not found: {databasePath}");
    return 1;
}

// Mirrors TightWiki\Program.cs's SQLite bootstrap: the Guid type handler has to be registered before
// anything touches Dapper.
SqlMapper.AddTypeHandler(new GuidTypeHandler());

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DatabasePath"] = databasePath,
    })
    .Build();

Console.WriteLine($"Applying schema upgrade scripts against: {databasePath}");

ITwDatabaseManager databaseManager = new DatabaseManager(configuration);

var wasUpgraded = await databaseManager.InitializeSchema();

Console.WriteLine(wasUpgraded
    ? "Schema upgrade scripts were applied; the database directory is now at the latest version."
    : "The database directory was already at the latest version; no upgrade scripts were applied.");

return 0;

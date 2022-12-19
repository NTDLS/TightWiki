using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using TightWiki.Shared.ADO;

namespace CreateDatabaseSeeds
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: CreateDatabaseSeeds.exe <path_with_scripts>");
            }

            string scriptPath = args[0];
            string outputPath = args[1];


            var appSettingsJson = AppSettingsJson.GetAppSettings();
            Singletons.ConnectionString = ConfigurationExtensions.GetConnectionString(appSettingsJson, "TightWikiADO");

            using var handler = new SqlConnectionHandler();
            var scriptFiles = Directory.GetFiles(scriptPath, "*.sql").ToList();

            foreach (var scriptFile in scriptFiles)
            {
                Console.WriteLine("Writing: " + Path.GetFileNameWithoutExtension(scriptFile));

                string scriptText = File.ReadAllText(scriptFile);
                using SqlCommand command = new SqlCommand(scriptText, handler.Connection);
                using SqlDataReader reader = command.ExecuteReader();
                using StreamWriter sw = File.CreateText(Path.Combine(outputPath, Path.GetFileName(scriptFile)));
                while (reader.Read())
                {
                    sw.WriteLine(reader.GetString(0));
                }
                sw.Close();
            }
        }
    }
}

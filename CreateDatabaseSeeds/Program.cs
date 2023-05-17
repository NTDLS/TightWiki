using Microsoft.Extensions.Configuration;
using System;
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
                Console.WriteLine("Usage: CreateDatabaseSeeds.exe <path_with_scripts> <output_path>");
            }

            string scriptPath = args[0];
            string outputPath = args[1];

            var appSettingsJson = AppSettingsJson.GetAppSettings();
            Singletons.ConnectionString = ConfigurationExtensions.GetConnectionString(appSettingsJson, "TightWikiADO");

            using var handler = new SqlConnectionHandler();
            var scriptFiles = Directory.GetFiles(scriptPath, "*.sql").ToList();

            int maxFileSize = 1024 * 1024 * 5;

            foreach (var scriptFile in scriptFiles)
            {
                Console.WriteLine("Writing: " + Path.GetFileNameWithoutExtension(scriptFile));

                int currentFileSize = 0;
                int currentFileCount = 1;

                string scriptText = File.ReadAllText(scriptFile);
                using SqlCommand command = new(scriptText, handler.Connection);
                using SqlDataReader reader = command.ExecuteReader();
                string newOutputFile = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(scriptFile)}_{currentFileCount:D4}{Path.GetExtension(scriptFile)}");
                StreamWriter sw = File.CreateText(newOutputFile);
                while (reader.Read())
                {
                    var currentLine = reader.GetString(0);
                    sw.WriteLine(currentLine);

                    if (currentFileSize >= maxFileSize && currentLine.ToLower() == "go")
                    {
                        currentFileSize = 0;
                        currentFileCount++;
                        sw.Close();
                        sw.Dispose();

                        newOutputFile = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(scriptFile)}_{currentFileCount:D5}{Path.GetExtension(scriptFile)}");
                        sw = File.CreateText(newOutputFile);
                    }

                    currentFileSize += currentLine.Length;
                }
                sw.Close();
                sw.Dispose();
            }
        }
    }
}

using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
	public static partial class ConfigurationEntryRepository
	{        
		public static List<ConfigurationEntry> GetAllConfigurationEntry()
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<ConfigurationEntry>("GetAllConfigurationEntry",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
		}

		public static ConfigurationEntry GetConfigurationEntryById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<ConfigurationEntry>("GetConfigurationEntryById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
		}

		public static void UpdateConfigurationEntryById(ConfigurationEntry item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Id = item.Id,
					ConfigurationGroupId = item.ConfigurationGroupId,
					Name = item.Name,
					Value = item.Value,
					Description = item.Description
				};

                handler.Connection.Execute("UpdateConfigurationEntryById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}		

		public static int InsertConfigurationEntry(ConfigurationEntry item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					ConfigurationGroupId = item.ConfigurationGroupId,
					Name = item.Name,
					Value = item.Value,
					Description = item.Description
				};

                return handler.Connection.ExecuteScalar<int>("InsertConfigurationEntry",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}
		
		public static void DeleteConfigurationEntryById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeleteConfigurationEntryById",
                    new { Id = id }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}


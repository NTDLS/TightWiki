using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
	public static partial class ConfigurationGroupRepository
	{        
		public static List<ConfigurationGroup> GetAllConfigurationGroup()
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<ConfigurationGroup>("GetAllConfigurationGroup",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
		}

		public static ConfigurationGroup GetConfigurationGroupById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<ConfigurationGroup>("GetConfigurationGroupById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
		}

		public static void UpdateConfigurationGroupById(ConfigurationGroup item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Id = item.Id,
					Name = item.Name
				};

                handler.Connection.Execute("UpdateConfigurationGroupById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}		

		public static int InsertConfigurationGroup(ConfigurationGroup item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Name = item.Name
				};

                return handler.Connection.ExecuteScalar<int>("InsertConfigurationGroup",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}
		
		public static void DeleteConfigurationGroupById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeleteConfigurationGroupById",
                    new { Id = id }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}


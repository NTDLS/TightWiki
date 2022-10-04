using SharpWiki.Shared.ADO;
using SharpWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SharpWiki.Shared.Repository
{
	public static partial class PageRepository
	{        
		public static List<Page> GetAllPage()
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<Page>("GetAllPage",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
		}

		public static Page GetPageById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<Page>("GetPageById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
		}

		public static void UpdatePageById(Page item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Id = item.Id,
					Name = item.Name,
					Navigation = item.Navigation,
					Description = item.Description,
					Body = item.Body,
					CachedBody = item.CachedBody,
					CreatedByUserId = item.CreatedByUserId,
					CreatedDate = item.CreatedDate,
					ModifiedByUserId = item.ModifiedByUserId,
					ModifiedDate = item.ModifiedDate
				};

                handler.Connection.Execute("UpdatePageById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}		

		public static int InsertPage(Page item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Name = item.Name,
					Navigation = item.Navigation,
					Description = item.Description,
					Body = item.Body,
					CachedBody = item.CachedBody,
					CreatedByUserId = item.CreatedByUserId,
					CreatedDate = item.CreatedDate,
					ModifiedByUserId = item.ModifiedByUserId,
					ModifiedDate = item.ModifiedDate
				};

                return handler.Connection.ExecuteScalar<int>("InsertPage",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}
		
		public static void DeletePageById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeletePageById",
                    new { Id = id }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}


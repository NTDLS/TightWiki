using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
	public static partial class PageTagRepository
	{        
		public static List<PageTag> GetAllPageTag()
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageTag>("GetAllPageTag",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
		}

		public static PageTag GetPageTagById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageTag>("GetPageTagById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
		}

		public static void UpdatePageTagById(PageTag item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Id = item.Id,
					PageId = item.PageId,
					Tag = item.Tag
				};

                handler.Connection.Execute("UpdatePageTagById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}		

		public static int InsertPageTag(PageTag item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					PageId = item.PageId,
					Tag = item.Tag
				};

                return handler.Connection.ExecuteScalar<int>("InsertPageTag",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}
		
		public static void DeletePageTagById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeletePageTagById",
                    new { Id = id }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}


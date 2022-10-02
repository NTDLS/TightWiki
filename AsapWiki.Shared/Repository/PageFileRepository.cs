using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
	public static partial class PageFileRepository
	{        
		public static List<PageFile> GetAllPageFile()
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFile>("GetAllPageFile",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
		}

		public static PageFile GetPageFileById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFile>("GetPageFileById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
		}

		public static void UpdatePageFileById(PageFile item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Id = item.Id,
					PageId = item.PageId,
					Name = item.Name,
					ContentType = item.ContentType,
					Size = item.Size,
					CreatedDate = item.CreatedDate,
					Data = item.Data
				};

                handler.Connection.Execute("UpdatePageFileById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}		

		public static int InsertPageFile(PageFile item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					PageId = item.PageId,
					Name = item.Name,
					ContentType = item.ContentType,
					Size = item.Size,
					CreatedDate = item.CreatedDate,
					Data = item.Data
				};

                return handler.Connection.ExecuteScalar<int>("InsertPageFile",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}
		
		public static void DeletePageFileById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeletePageFileById",
                    new { Id = id }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}


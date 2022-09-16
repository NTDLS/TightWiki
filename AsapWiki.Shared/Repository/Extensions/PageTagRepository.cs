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

		public static void UpdatePageTags(int pageId, List<string> tags)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					PageId = pageId,
					Tags = string.Join(",", tags)
				};

                handler.Connection.Execute("UpdatePageTags",
					param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}


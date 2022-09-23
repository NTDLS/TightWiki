using AsapWiki.Shared.ADO;
using Dapper;
using System.Collections.Generic;
using System.Data;

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


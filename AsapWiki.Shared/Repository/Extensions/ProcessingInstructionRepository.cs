using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class ProcessingInstructionRepository
	{
		public static void UpdatePageProcessingInstructions(int pageId, List<string> instructions)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					PageId = pageId,
					Instructions = string.Join(",", instructions)
				};

				handler.Connection.Execute("UpdatePageProcessingInstructions",
					param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
			}
		}

		public static List<ProcessingInstruction> GetPageProcessingInstructionsByPageId(int pageId)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					PageId = pageId
				};

				return handler.Connection.Query<ProcessingInstruction>("GetPageProcessingInstructionsByPageId",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
			}
		}
	}
}

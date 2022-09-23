using AsapWiki.Shared.ADO;
using Dapper;
using System.Collections.Generic;
using System.Data;

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
	}
}

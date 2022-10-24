using System;

namespace TightWiki.Shared.Models.Data
{
	public partial class PageFileAttachment
	{
		public int Id { get; set; }
		public int PageId { get; set; }
		public string Name { get; set; }
		public string ContentType { get; set; }
		public long Size { get; set; }
		public DateTime CreatedDate { get; set; }
		public byte[] Data { get; set; }
		public string FileNavigation { get; set; }
		public string PageNavigation { get; set; }

		public string FriendlySize
		{
			get
			{
				return Wiki.WikiUtility.GetFriendlySize(Size);
			}
		}
	}
}
using System;
using System.Runtime.Serialization;

namespace AsapWiki.Shared.Models
{
	public partial class PageFile : BaseModel
	{
		public string FriendlySize
		{
			get
			{
				return Wiki.Utility.GetFriendlySize(Size);
			}
		}
	}
}

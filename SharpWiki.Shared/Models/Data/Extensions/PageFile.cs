using System;
using System.Runtime.Serialization;

namespace SharpWiki.Shared.Models
{
	public partial class PageFile
	{
		public string FriendlySize
		{
			get
			{
				return Wiki.WikiUtility.GetFriendlySize(Size);
			}
		}
	}
}

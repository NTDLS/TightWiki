using System;
using System.Runtime.Serialization;

namespace SharpWiki.Shared.Models.Data
{
	public partial class PageTag
	{
		#region Properties
		
private int _id;
		public int Id
		{
			get
			{
				return this._id;
			}
			set
			{
				if (this._id != value)
				{
					this._id = value;
				}            
			}
		}
		
private int _pageId;
		public int PageId
		{
			get
			{
				return this._pageId;
			}
			set
			{
				if (this._pageId != value)
				{
					this._pageId = value;
				}            
			}
		}
		
private string _tag;
		public string Tag
		{
			get
			{
				return this._tag;
			}
			set
			{
				if (this._tag != value)
				{
					this._tag = value;
				}            
			}
		}
			
		#endregion
	}
}

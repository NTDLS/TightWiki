using System;
using System.Runtime.Serialization;

namespace SharpWiki.Shared.Models.Data
{
	public partial class PageFile
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
		
private string _name;
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (this._name != value)
				{
					this._name = value;
				}            
			}
		}
		
private string _navigation;
		public string Navigation
		{
			get
			{
				return this._navigation;
			}
			set
			{
				if (this._navigation != value)
				{
					this._navigation = value;
				}            
			}
		}
		
private int _revision;
		public int Revision
		{
			get
			{
				return this._revision;
			}
			set
			{
				if (this._revision != value)
				{
					this._revision = value;
				}            
			}
		}
		
private DateTime _createdDate;
		public DateTime CreatedDate
		{
			get
			{
				return this._createdDate;
			}
			set
			{
				if (this._createdDate != value)
				{
					this._createdDate = value;
				}            
			}
		}
			
		#endregion
	}
}

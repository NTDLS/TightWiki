using System;
using System.Runtime.Serialization;

namespace TightWiki.Shared.Models
{
	public partial class PageFile : BaseModel
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
		
private string _contentType;
		public string ContentType
		{
			get
			{
				return this._contentType;
			}
			set
			{
				if (this._contentType != value)
				{
					this._contentType = value;
				}            
			}
		}
		
private int _size;
		public int Size
		{
			get
			{
				return this._size;
			}
			set
			{
				if (this._size != value)
				{
					this._size = value;
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
		
private byte[] _data;
		public byte[] Data
		{
			get
			{
				return this._data;
			}
			set
			{
				if (this._data != value)
				{
					this._data = value;
				}            
			}
		}
			
		#endregion
	}
}

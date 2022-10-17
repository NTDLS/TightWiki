using System;
using System.Runtime.Serialization;

namespace TightWiki.Shared.Models
{
	public partial class Page : BaseModel
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
		
private string _description;
		public string Description
		{
			get
			{
				return this._description;
			}
			set
			{
				if (this._description != value)
				{
					this._description = value;
				}            
			}
		}
		
private string _body;
		public string Body
		{
			get
			{
				return this._body;
			}
			set
			{
				if (this._body != value)
				{
					this._body = value;
				}            
			}
		}
		
private string _cachedBody;
		public string CachedBody
		{
			get
			{
				return this._cachedBody;
			}
			set
			{
				if (this._cachedBody != value)
				{
					this._cachedBody = value;
				}            
			}
		}
		
private int _createdByUserId;
		public int CreatedByUserId
		{
			get
			{
				return this._createdByUserId;
			}
			set
			{
				if (this._createdByUserId != value)
				{
					this._createdByUserId = value;
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
		
private int _modifiedByUserId;
		public int ModifiedByUserId
		{
			get
			{
				return this._modifiedByUserId;
			}
			set
			{
				if (this._modifiedByUserId != value)
				{
					this._modifiedByUserId = value;
				}            
			}
		}
		
private DateTime _modifiedDate;
		public DateTime ModifiedDate
		{
			get
			{
				return this._modifiedDate;
			}
			set
			{
				if (this._modifiedDate != value)
				{
					this._modifiedDate = value;
				}            
			}
		}
			
		#endregion
	}
}

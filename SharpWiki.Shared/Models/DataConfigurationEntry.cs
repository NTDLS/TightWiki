using System;
using System.Runtime.Serialization;

namespace SharpWiki.Shared.Models
{
	public partial class ConfigurationEntry : BaseModel
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
		
private int _configurationGroupId;
		public int ConfigurationGroupId
		{
			get
			{
				return this._configurationGroupId;
			}
			set
			{
				if (this._configurationGroupId != value)
				{
					this._configurationGroupId = value;
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
		
private string _value;
		public string Value
		{
			get
			{
				return this._value;
			}
			set
			{
				if (this._value != value)
				{
					this._value = value;
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
			
		#endregion
	}
}

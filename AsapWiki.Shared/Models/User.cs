using System;
using System.Runtime.Serialization;

namespace AsapWiki.Shared.Models
{
	public partial class User : BaseModel
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
		
private string _emailAddress;
		public string EmailAddress
		{
			get
			{
				return this._emailAddress;
			}
			set
			{
				if (this._emailAddress != value)
				{
					this._emailAddress = value;
				}            
			}
		}
		
private string _AccountName;
		public string AccountName
		{
			get
			{
				return this._AccountName;
			}
			set
			{
				if (this._AccountName != value)
				{
					this._AccountName = value;
				}            
			}
		}
		
private string _passwordHash;
		public string PasswordHash
		{
			get
			{
				return this._passwordHash;
			}
			set
			{
				if (this._passwordHash != value)
				{
					this._passwordHash = value;
				}            
			}
		}
			
		#endregion
	}
}

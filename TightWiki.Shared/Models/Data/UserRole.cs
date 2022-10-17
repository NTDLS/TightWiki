using System;
using System.Runtime.Serialization;

namespace TightWiki.Shared.Models.Data
{
	public partial class UserRole
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
		
private int _userId;
		public int UserId
		{
			get
			{
				return this._userId;
			}
			set
			{
				if (this._userId != value)
				{
					this._userId = value;
				}            
			}
		}
		
private int _roleId;
		public int RoleId
		{
			get
			{
				return this._roleId;
			}
			set
			{
				if (this._roleId != value)
				{
					this._roleId = value;
				}            
			}
		}
			
		#endregion
	}
}

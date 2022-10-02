using System;
using System.Runtime.Serialization;

namespace AsapWiki.Shared.Models
{
	public partial class MenuItem
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
		
private string _link;
		public string Link
		{
			get
			{
				return this._link;
			}
			set
			{
				if (this._link != value)
				{
					this._link = value;
				}            
			}
		}
		
private int _ordinal;
		public int Ordinal
		{
			get
			{
				return this._ordinal;
			}
			set
			{
				if (this._ordinal != value)
				{
					this._ordinal = value;
				}            
			}
		}
			
		#endregion
	}
}

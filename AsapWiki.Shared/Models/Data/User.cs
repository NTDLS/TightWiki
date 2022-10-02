using System;
using System.Runtime.Serialization;

namespace AsapWiki.Shared.Models
{
	public partial class User
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

		private string _accountName;
		public string AccountName
		{
			get
			{
				return this._accountName;
			}
			set
			{
				if (this._accountName != value)
				{
					this._accountName = value;
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

		private string _firstName;
		public string FirstName
		{
			get
			{
				return this._firstName;
			}
			set
			{
				if (this._firstName != value)
				{
					this._firstName = value;
				}
			}
		}

		private string _lastName;
		public string LastName
		{
			get
			{
				return this._lastName;
			}
			set
			{
				if (this._lastName != value)
				{
					this._lastName = value;
				}
			}
		}

		private string _timeZone;
		public string TimeZone
		{
			get
			{
				return this._timeZone;
			}
			set
			{
				if (this._timeZone != value)
				{
					this._timeZone = value;
				}
			}
		}

		private string _country;
		public string Country
		{
			get
			{
				return this._country;
			}
			set
			{
				if (this._country != value)
				{
					this._country = value;
				}
			}
		}

		private string _aboutMe;
		public string AboutMe
		{
			get
			{
				return this._aboutMe;
			}
			set
			{
				if (this._aboutMe != value)
				{
					this._aboutMe = value;
				}
			}
		}

		private byte[] _avatar;
		public byte[] Avatar
		{
			get
			{
				return this._avatar;
			}
			set
			{
				if (this._avatar != value)
				{
					this._avatar = value;
				}
			}
		}

		private DateTime? _createdDate;
		public DateTime? CreatedDate
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

		private DateTime? _modifiedDate;
		public DateTime? ModifiedDate
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

		private DateTime? _lastLoginDate;
		public DateTime? LastLoginDate
		{
			get
			{
				return this._lastLoginDate;
			}
			set
			{
				if (this._lastLoginDate != value)
				{
					this._lastLoginDate = value;
				}
			}
		}

		private string _verificationCode;
		public string VerificationCode
		{
			get
			{
				return this._verificationCode;
			}
			set
			{
				if (this._verificationCode != value)
				{
					this._verificationCode = value;
				}
			}
		}

		private bool? _emailVerified;
		public bool? EmailVerified
		{
			get
			{
				return this._emailVerified;
			}
			set
			{
				if (this._emailVerified != value)
				{
					this._emailVerified = value;
				}
			}
		}

		#endregion
	}
}
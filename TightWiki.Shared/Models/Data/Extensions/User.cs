using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TightWiki.Shared.Models.Data
{
    public partial class User
	{
		public int Id { get; set; }
		public string EmailAddress { get; set; }
		public string AccountName { get; set; }
		public string Navigation { get; set; }
		public string PasswordHash { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string TimeZone { get; set; }
		public string Country { get; set; }
		public string AboutMe { get; set; }
		public byte[] Avatar { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime ModifiedDate { get; set; }
		public DateTime LastLoginDate { get; set; }
		public string VerificationCode { get; set; }
		public bool EmailVerified { get; set; }
		public int PaginationSize { get; set; }
        public int PaginationCount { get; set; }
    }
}


using System;

namespace TightWiki.DataModels
{
    public partial class UserRole
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int RoleId { get; set; }
    }
}
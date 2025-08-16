﻿using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public class AccountsViewModel : ViewModelBase
    {
        public List<AccountProfile> Users { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}

﻿namespace TightWiki.Models.ViewModels.Admin
{
    public class DeletedPagesViewModel : ViewModelBase
    {
        public List<DataModels.Page> Pages { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}

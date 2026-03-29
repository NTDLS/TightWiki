// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TightWiki.Plugin;

namespace TightWiki.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel
        : PageModel
    {
        private readonly TwConfiguration _wikiConfiguration;

        public IndexModel(TwConfiguration wikiConfiguration)
        {
            _wikiConfiguration = wikiConfiguration;
        }

        public IActionResult OnGet()
        {
            return Redirect($"{_wikiConfiguration.BasePath}/Identity/Account/Manage/Email");
        }
    }
}

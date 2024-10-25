// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TightWiki.Models;

namespace TightWiki.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGetAsync()
        {
            return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/Manage/Email");
        }
    }
}

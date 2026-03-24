// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;

namespace TightWiki.Areas.Identity.Pages.Account
{
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class AccessDeniedModel : PageModelBase
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public void OnGet()
        {
        }
        public AccessDeniedModel(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager, ISharedLocalizationText localizer)
            : base(logger, signInManager, localizer)
        {

        }
    }
}

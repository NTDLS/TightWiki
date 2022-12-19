using TightWiki.Controllers;
using TightWiki.Shared.Library;
using TightWiki.Shared.Repository;

namespace TightWiki
{
    public static class Singletons
    {
        /// <summary>
        /// Detect whether this is the first time the WIKI has ever been run and do some initilization.
        /// </summary>
        public static void DoFirstRun(ControllerHelperBase controller)
        {
            if (ConfigurationRepository.IsFirstRun(Constants.CRYPTOCHECK, Security.MachineKey))
            {
                //If this is the first time we have ever run on this server, set the password to default.
                var adminUser = UserRepository.GetUserByNavigation("admin");
                if (adminUser != null)
                {
                    UserRepository.UpdateUserPassword(adminUser.Id, Constants.DEFAULTPASSWORD);
                }

                var pages = PageRepository.GetAllPages();

                foreach (var page in pages)
                {
                    controller.SavePage(page);
                }
            }
        }
    }
}

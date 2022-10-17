using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class UserController : ControllerHelperBase
    {
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Logout()
        {
            Session.Abandon();
            FormsAuthentication.SignOut();
            return RedirectToAction("Display", "Page", "Home");
        }

        [HttpGet]
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordModel());
        }

        [HttpPost]
        [Authorize]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if ((model.Password?.Length ?? 0) < 5)
            {
                ModelState.AddModelError("Password", "The password is too short. 5 character minimum.");
                return View(model);
            }
            else if ((model.ComparePassword?.Length ?? 0) < 5)
            {
                ModelState.AddModelError("ComparePassword", "The password is too short. 5 character minimum.");
                return View(model);
            }
            else if (model.Password != model.ComparePassword)
            {
                ModelState.AddModelError("ComparePassword", "The passwords you entered do not match");
                return View(model);
            }
            else if (ModelState.IsValid)
            {
                UserRepository.UpdateUserPassword(context.User.Id, model.Password);
                ViewBag.Success = "Your account password has been changed!";
            }

            return View(model);
        }

        //Populate login form.
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Login user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Login(LoginModel user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    PerformLogin(user.EmailAddress, user.Password, false);

                    if (Request.QueryString["ReturnUrl"] != null && Request.QueryString["ReturnUrl"] != "/")
                    {
                        return Redirect(Request.QueryString["ReturnUrl"]);
                    }
                    else
                    {
                        return RedirectToAction("Display", "Page", "Home");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View();
        }

        /// <summary>
        /// Populate forgot password form.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Forgot()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Forgot(ForgotModel model)
        {
            var user = UserRepository.GetUserByEmail(model.EmailAddress);

            if (user != null)
            {
                var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
                var siteName = basicConfig.ValueAs<string>("Name");
                var address = basicConfig.ValueAs<string>("Address");

                var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
                var resetPasswordEmailTemplate = new StringBuilder(membershipConfig.ValueAs<string>("Reset Password Email Template"));

                user.VerificationCode = Security.GenerateRandomString(6);
                UserRepository.UpdateUserVerificationCode(user.Id, user.VerificationCode);

                var emailSubject = "Reset Password";
                resetPasswordEmailTemplate.Replace("##SUBJECT##", emailSubject);
                resetPasswordEmailTemplate.Replace("##ACCOUNTCOUNTRY##", user.Country);
                resetPasswordEmailTemplate.Replace("##ACCOUNTTIMEZONE##", user.TimeZone);
                resetPasswordEmailTemplate.Replace("##ACCOUNTEMAIL##", user.EmailAddress);
                resetPasswordEmailTemplate.Replace("##ACCOUNTNAME##", user.AccountName);
                resetPasswordEmailTemplate.Replace("##PERSONNAME##", $"{user.FirstName} {user.LastName}");
                resetPasswordEmailTemplate.Replace("##CODE##", user.VerificationCode);
                resetPasswordEmailTemplate.Replace("##SITENAME##", siteName);
                resetPasswordEmailTemplate.Replace("##SITEADDRESS##", address);

                Email.Send(user.EmailAddress, emailSubject, resetPasswordEmailTemplate.ToString());
            }

            return RedirectToAction("PasswordResetEmailSent", "User");
        }

        /// <summary>
        /// Populate reset password form.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Reset(string userAccountName, string verificationCode)
        {
            var user = UserRepository.GetUserByNavigation(userAccountName);

            if (user != null)
            {
                if (user.VerificationCode?.ToLower() != verificationCode.ToLower())
                {
                    ViewBag.Error = "The verification code you specified can not be found.";
                }
                else
                {
                    return View(new ResetModel
                    {
                        EmailAddress = user.EmailAddress,
                        VerificationCode = verificationCode
                    });
                }
            }
            else
            {
                ViewBag.Error = "The email address you specified can not be found.";
            }

            return View(new ResetModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Reset(string userAccountName, string verificationCode, ResetModel model)
        {
            if (ModelState.IsValid)
            {
                var user = UserRepository.GetUserByEmail(model.EmailAddress);

                if (user != null)
                {
                    if (user.VerificationCode?.ToLower() != model.VerificationCode.ToLower())
                    {
                        ViewBag.Error = "The verification code you specified can not be found.";
                        return Reset(userAccountName, verificationCode);
                    }

                    UserRepository.UpdateUserPassword(user.Id, model.Password);
                    UserRepository.VerifyUserEmail(user.Id);

                    return RedirectToAction("ResetComplete", "User");
                }
                else
                {
                    ViewBag.Error = "The email address you specified can not be found.";
                }

                return View(new ResetModel());
            }

            return Reset(userAccountName, verificationCode);
        }

        /// <summary>
        /// Populate signup page.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Signup()
        {
            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();

            if (ConfigurationRepository.Get("Membership", "Allow Signup", false) == false)
            {
                return new HttpUnauthorizedResult();
            }

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");

            var model = new SignupModel()
            {
                Country = basicConfig.ValueAs<string>("Default Country"),
                TimeZone = basicConfig.ValueAs<string>("Default TimeZone"),
            };

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ResetComplete()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SignupComplete()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SignupCompleteVerification()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SignupPendingVerification()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult PasswordResetEmailSent()
        {
            return View();
        }

        /// <summary>
        /// Save signup form.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Signup(SignupModel model)
        {
            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();

            if (ConfigurationRepository.Get("Membership", "Allow Signup", false) == false)
            {
                return new HttpUnauthorizedResult();
            }

            model.EmailAddress = HTML.StripHTML(model.EmailAddress);
            model.AccountName = HTML.StripHTML(model.AccountName);

            if ((model.EmailAddress?.Length ?? 0) < 5)
            {
                ModelState.AddModelError("EmailAddress", "You must enter an email address.");
                return View(model);
            }
            else if (WikiUtility.IsValidEmail(model.EmailAddress) == false)
            {
                ModelState.AddModelError("EmailAddress", "You must specifiy a valid email address.");
                return View(model);
            }
            else if (UserRepository.DoesEmailAddressExist(model.EmailAddress))
            {
                ModelState.AddModelError("EmailAddress", "This email address is already in use.");
                return View(model);
            }
            else if ((model.AccountName?.Length ?? 0) < 2)
            {
                ModelState.AddModelError("AccountName", "You must enter an account name / alias.");
                return View(model);
            }
            else if (UserRepository.DoesAccountNameExist(model.AccountName))
            {
                ModelState.AddModelError("AccountName", "This display name is already in use.");
                return View(model);
            }
            else if ((model.Password?.Length ?? 0) < 5)
            {
                ModelState.AddModelError("Password", "The password is too short. 5 character minimum.");
                return View(model);
            }
            else if ((model.ComparePassword?.Length ?? 0) < 5)
            {
                ModelState.AddModelError("ComparePassword", "The password is too short. 5 character minimum.");
                return View(model);
            }
            else if (model.Password != model.ComparePassword)
            {
                ModelState.AddModelError("ComparePassword", "The passwords you entered do not match");
                return View(model);
            }
            else if (ModelState.IsValid)
            {
                var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
                var siteName = basicConfig.ValueAs<string>("Name");
                var address = basicConfig.ValueAs<string>("Address");

                var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
                var defaultSignupRole = membershipConfig.ValueAs<string>("Default Signup Role");
                var requestEmailVerification = membershipConfig.ValueAs<bool>("Request Email Verification");
                var requireEmailVerification = membershipConfig.ValueAs<bool>("Require Email Verification");
                var accountVerificationEmailTemplate = new StringBuilder(membershipConfig.ValueAs<string>("Account Verification Email Template"));

                var user = new User()
                {
                    EmailAddress = model.EmailAddress,
                    AccountName = model.AccountName,
                    Navigation = WikiUtility.CleanPartialURI(model.AccountName),
                    PasswordHash = Security.Sha256(model.Password),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    TimeZone = model.TimeZone,
                    Country = model.Country,
                    AboutMe = string.Empty,
                    VerificationCode = Security.GenerateRandomString(6)
                };

                int userId = UserRepository.CreateUser(user);

                if (requestEmailVerification || requireEmailVerification)
                {
                    var emailSubject = "Account Verification";
                    accountVerificationEmailTemplate.Replace("##SUBJECT##", emailSubject);
                    accountVerificationEmailTemplate.Replace("##ACCOUNTCOUNTRY##", user.Country);
                    accountVerificationEmailTemplate.Replace("##ACCOUNTTIMEZONE##", user.TimeZone);
                    accountVerificationEmailTemplate.Replace("##ACCOUNTEMAIL##", user.EmailAddress);
                    accountVerificationEmailTemplate.Replace("##ACCOUNTNAME##", user.AccountName);
                    accountVerificationEmailTemplate.Replace("##PERSONNAME##", $"{user.FirstName} {user.LastName}");
                    accountVerificationEmailTemplate.Replace("##CODE##", user.VerificationCode);
                    accountVerificationEmailTemplate.Replace("##SITENAME##", siteName);
                    accountVerificationEmailTemplate.Replace("##SITEADDRESS##", address);

                    Email.Send(user.EmailAddress, emailSubject, accountVerificationEmailTemplate.ToString());
                }

                UserRepository.UpdateUserRoles(userId, defaultSignupRole);

                if (requireEmailVerification)
                {
                    return RedirectToAction("SignupPendingVerification", "User");
                }
                else if (requestEmailVerification)
                {
                    return RedirectToAction("SignupCompleteVerification", "User");
                }
                else {
                    return RedirectToAction("SignupComplete", "User");
                }
            }

            return View(model);
        }

        /// <summary>
        /// Confirms an account email address
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Confirm(string userAccountName, string verificationCode)
        {
            userAccountName = WikiUtility.CleanPartialURI(userAccountName);

            var user = UserRepository.GetUserByNavigationAndVerificationCode(userAccountName, verificationCode);

            if (user == null)
            {
                ViewBag.Error = "The account and verification code you specified could not be found.";
            }
            else
            {
                UserRepository.VerifyUserEmail(user.Id);
                ViewBag.Success = "Your account has been confirmed, feel free to login!";

                try
                {
                    PerformLogin(user.EmailAddress, user.PasswordHash, true);

                    if (Request.QueryString["ReturnUrl"] != null && Request.QueryString["ReturnUrl"] != "/")
                    {
                        return Redirect(Request.QueryString["ReturnUrl"]);
                    }
                    else
                    {
                        return RedirectToAction("Display", "Page", "Home");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }

            }

            return View();
        }

        /// <summary>
        /// //Gets a users avatar.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Avatar(string userAccountName)
        {
            if (context.CanView == false)
            {
                return new HttpUnauthorizedResult();
            }

            userAccountName = WikiUtility.CleanPartialURI(userAccountName);
            string scale = Request.QueryString["Scale"] ?? "100";
            string max = Request.QueryString["max"];

            byte[] imageBytes = UserRepository.GetUserAvatarBynavigation(userAccountName);
            if (imageBytes != null && imageBytes.Count() > 0)
            {
                var img = System.Drawing.Image.FromStream(new MemoryStream(imageBytes));

                int iScale = int.Parse(scale);
                int iMax = int.Parse(max);

                if (iMax != 0 && (img.Width > iMax || img.Height > iMax))
                {
                    int diff = img.Width - iMax;
                    int width = (int)(img.Width - diff);
                    int height = (int)(img.Height - diff);
                    if (height < 1)
                    {
                        height = iMax;
                    }

                    using (var bmp = Images.ResizeImage(img, width, height))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, ImageFormat.Png);
                            return File(ms.ToArray(), "image/png");
                        }
                    }

                }
                else if (iScale != 100)
                {
                    int width = (int)(img.Width * (iScale / 100.0));
                    int height = (int)(img.Height * (iScale / 100.0));

                    using (var bmp = Images.ResizeImage(img, width, height))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, ImageFormat.Png);
                            return File(ms.ToArray(), "image/png");
                        }
                    }
                }
                else
                {
                    var bmp = img as System.Drawing.Bitmap;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        return File(ms.ToArray(), "image/png");
                    }
                }
            }
            else
            {
                return new HttpNotFoundResult();
            }
        }

        /// <summary>
        /// Get user profile.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public ActionResult UserProfile()
        {
            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();

            var user = UserRepository.GetUserById(context.User.Id);

            var profile = new UserProfileModel()
            {
                AccountName = user.AccountName,
                EmailAddress = user.EmailAddress,
                Navigation = user.Navigation,
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TimeZone = user.TimeZone,
                Country = user.Country,
                AboutMe = user.AboutMe,
                Avatar = user.Avatar
            };

            return View(profile);
        }


        /// <summary>
        /// Save user profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult UserProfile([Bind(Exclude = "Avatar")] UserProfileModel model)
        {
            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();

            model.Navigation = WikiUtility.CleanPartialURI(model.AccountName.ToLower());
            var user = UserRepository.GetUserById(context.User.Id);

            HttpPostedFileBase file = Request.Files["Avatar"];
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    var imageBytes = Utility.ConvertHttpFileToBytes(file);
                    //This is just to ensure this is a valid image:
                    var image = System.Drawing.Image.FromStream(new MemoryStream(imageBytes));
                    UserRepository.UpdateUserAvatar(user.Id, imageBytes);
                }
                catch
                {
                    ModelState.AddModelError("Avatar", "Could not save the attached image.");
                }
            }

            if (ModelState.IsValid)
            {
                if (user.Navigation.ToLower() != model.Navigation.ToLower())
                {
                    var checkName = UserRepository.GetUserByNavigation(WikiUtility.CleanPartialURI(model.AccountName.ToLower()));
                    if (checkName != null)
                    {
                        ModelState.AddModelError("AccountName", "Account name is already in use.");
                        return View(model);
                    }
                }

                if (user.EmailAddress.ToLower() != model.EmailAddress.ToLower())
                {
                    var checkName = UserRepository.GetUserByEmail(model.EmailAddress.ToLower());
                    if (checkName != null)
                    {
                        ModelState.AddModelError("EmailAddress", "Email address is already in use.");
                        return View(model);
                    }
                }

                user.AboutMe = model.AboutMe;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.TimeZone = model.TimeZone;
                user.Country = model.Country;
                user.AccountName = model.AccountName;
                user.Navigation = WikiUtility.CleanPartialURI(model.AccountName);
                user.EmailAddress = model.EmailAddress;
                user.ModifiedDate = DateTime.UtcNow;
                UserRepository.UpdateUser(user);

                ViewBag.Success = "Your profile has been saved successfully!.";
            }

            return View(model);
        }
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using TightWiki.Controllers;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class AccountController : ControllerHelperBase
    {
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Logout()
        {
            HttpContext.SignOutAsync();
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
            var model = new LoginModel();
            return View(model);
        }

        /// <summary>
        /// Login user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    PerformLogin(model.EmailAddress, model.Password, false);

                    if (Request.Query["ReturnUrl"].ToString().IsNullOrEmpty() == false && Request.Query["ReturnUrl"] != "/")
                    {
                        return Redirect(Request.Query["ReturnUrl"]);
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
            return View(model);
        }

        /// <summary>
        /// Populate forgot password form.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Forgot()
        {
            var model = new ForgotModel();
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Forgot(ForgotModel model)
        {
            var user = UserRepository.GetUserByEmail(model.EmailAddress);

            if (user != null)
            {
                var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
                var siteName = basicConfig.As<string>("Name");
                var address = basicConfig.As<string>("Address");

                var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
                var resetPasswordEmailTemplate = new StringBuilder(membershipConfig.As<string>("Reset Password Email Template"));

                user.VerificationCode = Security.GenerateRandomString(6);
                UserRepository.UpdateUserVerificationCode(user.Id, user.VerificationCode);

                var emailSubject = "Reset Password";
                resetPasswordEmailTemplate.Replace("##SUBJECT##", emailSubject);
                resetPasswordEmailTemplate.Replace("##ACCOUNTCOUNTRY##", user.Country);
                resetPasswordEmailTemplate.Replace("##ACCOUNTTIMEZONE##", user.TimeZone);
                resetPasswordEmailTemplate.Replace("##ACCOUNTLANGUAGE##", user.Language);
                resetPasswordEmailTemplate.Replace("##ACCOUNTEMAIL##", user.EmailAddress);
                resetPasswordEmailTemplate.Replace("##ACCOUNTNAME##", user.AccountName);
                resetPasswordEmailTemplate.Replace("##PERSONNAME##", $"{user.FirstName} {user.LastName}");
                resetPasswordEmailTemplate.Replace("##CODE##", user.VerificationCode);
                resetPasswordEmailTemplate.Replace("##SITENAME##", siteName);
                resetPasswordEmailTemplate.Replace("##SITEADDRESS##", address);

                Email.Send(user.EmailAddress, emailSubject, resetPasswordEmailTemplate.ToString());
            }

            return RedirectToAction("PasswordResetEmailSent", "Account");
        }

        /// <summary>
        /// Populate reset password form.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Reset(string userAccountName, string verificationCode)
        {
            var model = new ResetModel();

            var user = UserRepository.GetUserByNavigation(userAccountName);

            if (user != null)
            {
                if (user.VerificationCode?.ToLower() != verificationCode.ToLower())
                {
                    model.ErrorMessage = "The verification code you specified can not be found.";
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
                model.ErrorMessage = "The email address you specified can not be found.";
            }

            return View(model);
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
                        model.ErrorMessage = "The verification code you specified can not be found.";
                        return Reset(userAccountName, verificationCode);
                    }

                    UserRepository.UpdateUserPassword(user.Id, model.Password);
                    UserRepository.VerifyUserEmail(user.Id);

                    return RedirectToAction("ResetComplete", "Account");
                }
                else
                {
                    model.ErrorMessage = "The email address you specified can not be found.";
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
            ViewBag.Languages = LanguageItem.GetAll();

            if (ConfigurationRepository.Get("Membership", "Allow Signup", false) == false)
            {
                return Unauthorized();
            }

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");

            var model = new SignupModel()
            {
                Country = basicConfig.As<string>("Default Country"),
                TimeZone = basicConfig.As<string>("Default TimeZone"),
                Language = basicConfig.As<string>("Default Language"),
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
            ViewBag.Languages = LanguageItem.GetAll();

            if (ConfigurationRepository.Get("Membership", "Allow Signup", false) == false)
            {
                return Unauthorized();
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
                var siteName = basicConfig.As<string>("Name");
                var address = basicConfig.As<string>("Address");

                var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
                var defaultSignupRole = membershipConfig.As<string>("Default Signup Role");
                var requestEmailVerification = membershipConfig.As<bool>("Request Email Verification");
                var requireEmailVerification = membershipConfig.As<bool>("Require Email Verification");
                var accountVerificationEmailTemplate = new StringBuilder(membershipConfig.As<string>("Account Verification Email Template"));

                var user = new User()
                {
                    EmailAddress = model.EmailAddress,
                    AccountName = model.AccountName,
                    Navigation = WikiUtility.CleanPartialURI(model.AccountName),
                    PasswordHash = Security.Sha256(model.Password),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    TimeZone = model.TimeZone,
                    Language = model.Language,
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
                    accountVerificationEmailTemplate.Replace("##ACCOUNTLANGUAGE##", user.Language);
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
                    return RedirectToAction("SignupPendingVerification", "Account");
                }
                else if (requestEmailVerification)
                {
                    return RedirectToAction("SignupCompleteVerification", "Account");
                }
                else {
                    return RedirectToAction("SignupComplete", "Account");
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
            var model = new ConfirmModel();

            userAccountName = WikiUtility.CleanPartialURI(userAccountName);

            var user = UserRepository.GetUserByNavigationAndVerificationCode(userAccountName, verificationCode);

            if (user == null)
            {
                model.ErrorMessage = "The account and verification code you specified could not be found.";
            }
            else
            {
                UserRepository.VerifyUserEmail(user.Id);
                model.SuccessMessage = "Your account has been confirmed, feel free to login!";

                try
                {
                    PerformLogin(user.EmailAddress, user.PasswordHash, true);

                    if (Request.Query["ReturnUrl"].ToString().IsNullOrEmpty() && Request.Query["ReturnUrl"] != "/")
                    {
                        return Redirect(Request.Query["ReturnUrl"]);
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

            return View(model);
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
                return Unauthorized();
            }

            userAccountName = WikiUtility.CleanPartialURI(userAccountName);
            string scale = Request.Query["Scale"].ToString().ToString().IsNullOrEmpty("100");
            string max = Request.Query["max"];

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
                return NotFound();
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
            ViewBag.Languages = LanguageItem.GetAll();

            var user = UserRepository.GetUserById(context.User.Id);

            var model = new UserProfileModel()
            {
                AccountName = user.AccountName,
                EmailAddress = user.EmailAddress,
                Navigation = user.Navigation,
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TimeZone = user.TimeZone,
                Language = user.Language,
                Country = user.Country,
                AboutMe = user.AboutMe,
                Avatar = user.Avatar
            };

            return View(model);
        }


        /// <summary>
        /// Save user profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult UserProfile(UserProfileModel model)
        {
            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();
            ViewBag.Languages = LanguageItem.GetAll();

            model.Navigation = WikiUtility.CleanPartialURI(model.AccountName.ToLower());
            var user = UserRepository.GetUserById(context.User.Id);

            var file = Request.Form.Files["Avatar"];
            if (file != null && file.Length > 0)
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
                user.Language = model.Language;
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

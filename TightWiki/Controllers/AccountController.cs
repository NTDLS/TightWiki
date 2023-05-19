using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TightWiki.Controllers;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;

namespace TightWiki.Site.Controllers
{
    //[Authorize]
    [AllowAnonymous]
    public class AccountController : ControllerHelperBase
    {
        [HttpPost]
        [AllowAnonymous]
        public IActionResult GoogleSignup()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("Signup", "Account") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            var returnUrl = Url.Action("Display", "Page", "Home");
            if (Request.Query["ReturnUrl"].ToString().IsNullOrEmpty() == false && Request.Query["ReturnUrl"] != "/")
            {
                returnUrl = Request.Query["ReturnUrl"];
            }

            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleLoginCallback", new { ReturnUrl = returnUrl }) };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleLoginCallback(string returnUrl)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                return RedirectToAction("Login", "Account", new { ReturnUrl = returnUrl });
            }

            var claims = authenticateResult.Principal.Claims.ToList();
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                RedirectUri = returnUrl
            };

            var result = HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme).Result;
            var firstIdentity = result.Principal.Identities.FirstOrDefault();
            var emailddress = firstIdentity.Claims.Where(o => o.Type == ClaimTypes.Email)?.FirstOrDefault().Value;

            var user = UserRepository.GetUserByEmail(emailddress);
            if (user == null)
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("AccountNotFound", "Account");
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            return LocalRedirect(returnUrl);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Logout()
        {
            ViewBag.Context.Title = $"Logout";
            HttpContext.SignOutAsync();

            if (Request.Query["ReturnUrl"].ToString().IsNullOrEmpty() == false && Request.Query["ReturnUrl"] != "/")
            {
                return Redirect(Request.Query["ReturnUrl"]);
            }
            else
            {
                return RedirectToAction("Display", "Page", "Home");
            }
        }

        [HttpGet]
        [Authorize]
        public ActionResult ChangePassword()
        {
            if (context.User.Provider != "Native")
            {
                throw new NotSupportedException();
            }
            ViewBag.Context.Title = $"Change Password";
            return View(new ChangePasswordModel());
        }

        [HttpPost]
        [Authorize]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (context.User.Provider != "Native")
            {
                throw new NotSupportedException();
            }

            ViewBag.Context.Title = $"Change Password";

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
                model.SuccessMessage = "Your account password has been changed!";
            }

            return View(model);
        }

        //Populate login form.
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            ViewBag.Context.Title = $"Login";
            ViewBag.AllowSignup = (ConfigurationRepository.Get("Membership", "Authorization: Allow Signup", false) == true);
            var model = new LoginModel();

            if (Request.Query["ReturnUrl"].ToString().IsNullOrEmpty() == false && Request.Query["ReturnUrl"] != "/")
            {
                ViewBag.ReturnUrl = Request.Query["ReturnUrl"];
            }

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
            ViewBag.Context.Title = $"Login";

            ViewBag.AllowSignup = (ConfigurationRepository.Get("Membership", "Authorization: Allow Signup", false) == true);

            if (ModelState.IsValid)
            {
                try
                {
                    PerformLogin(model.EmailAddress, model.Password, false, model.RememberMe);

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
            ViewBag.Context.Title = $"Forgot";
            var model = new ForgotModel();
            ViewBag.AllowSignup = (ConfigurationRepository.Get("Membership", "Authorization: Allow Signup", false) == true);
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Forgot(ForgotModel model)
        {
            ViewBag.Context.Title = $"Forgot";
            var user = UserRepository.GetUserByEmail(model.EmailAddress);
            ViewBag.AllowSignup = (ConfigurationRepository.Get("Membership", "Authorization: Allow Signup", false) == true);

            if (user != null)
            {
                var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
                var siteName = basicConfig.As<string>("Name");
                var address = basicConfig.As<string>("Address");

                if (user.Provider != "Native")
                {
                    //TODO: This should send an email to the user to let them know what their provider is.
                    throw new NotImplementedException();
                }

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
            ViewBag.Context.Title = $"Reset";
            var model = new ResetModel();

            var user = UserRepository.GetUserByNavigation(userAccountName);

            if (user != null)
            {
                if (user.Provider != "Native")
                {
                    //TODO: This should send an email to the user to let them know what their provider is.
                    throw new NotImplementedException();
                }

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
            ViewBag.Context.Title = $"Reset";
            if (ModelState.IsValid)
            {
                var user = UserRepository.GetUserByEmail(model.EmailAddress);

                if (user != null)
                {
                    if (user.Provider != "Native")
                    {
                        //TODO: This should send an email to the user to let them know what their provider is.
                        throw new NotImplementedException();
                    }

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
            ViewBag.Context.Title = $"Signup";
            if (ConfigurationRepository.Get("Membership", "Authorization: Allow Signup", false) == false)
            {
                return Unauthorized();
            }

            if (context.IsAuthenticated) //No need to create an account.
            {
                return RedirectToAction("UserProfile", "Account");
            }

            var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Customization");

            var model = new SignupModel()
            {
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Country = customizationConfig.As<string>("Localization: Default Country"),
                TimeZone = customizationConfig.As<string>("Localization: Default TimeZone"),
                Language = customizationConfig.As<string>("Localization: Default Language"),
            };

            if (context.IsPartiallyAuthenticated)
            {
                var result = HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme).Result;
                var firstIdentity = result.Principal.Identities.FirstOrDefault();

                model.EmailAddress = firstIdentity.Claims.Where(o => o.Type == ClaimTypes.Email)?.FirstOrDefault().Value;
                model.LastName = firstIdentity.Claims.Where(o => o.Type == ClaimTypes.Surname)?.FirstOrDefault().Value;
                model.FirstName = firstIdentity.Claims.Where(o => o.Type == ClaimTypes.GivenName)?.FirstOrDefault().Value;

                //If we are using an external provider, generate a nonsense password just because I am uncomfortable with empty passwords.
                model.Password = Guid.NewGuid().ToString();
                model.ComparePassword = model.Password;
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult AccountNotFound()
        {
            ViewBag.Context.Title = $"Account not found";

            ViewBag.AllowSignup = (ConfigurationRepository.Get("Membership", "Authorization: Allow Signup", false) == true);

            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult ResetComplete()
        {
            ViewBag.Context.Title = $"Reset Complete";
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SignupComplete()
        {
            ViewBag.Context.Title = $"Signup Complete";
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SignupCompleteVerification()
        {
            ViewBag.Context.Title = $"Signup Complete";
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SignupPendingVerification()
        {
            ViewBag.Context.Title = $"Signup Complete";
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult PasswordResetEmailSent()
        {
            ViewBag.Context.Title = $"Reset";
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
            ViewBag.Context.Title = $"Signup";
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();

            if (ConfigurationRepository.Get("Membership", "Authorization: Allow Signup", false) == false)
            {
                return Unauthorized();
            }

            if (context.IsAuthenticated) //No need to create an account.
            {
                return RedirectToAction("UserProfile", "Account");
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
                var defaultSignupRole = membershipConfig.As<string>("Authorization: Default Signup Role");
                var requestEmailVerification = membershipConfig.As<bool>("Authorization: Request Email Verification");
                var requireEmailVerification = membershipConfig.As<bool>("Authorization: Require Email Verification");
                var accountVerificationEmailTemplate = new StringBuilder(membershipConfig.As<string>("Template: Account Verification Email Template"));

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
                    Role = defaultSignupRole,
                    VerificationCode = Security.GenerateRandomString(6),
                    Provider = "Native"
                };

                if (context.IsPartiallyAuthenticated)
                {
                    user.Provider = User.Identity.AuthenticationType;
                }

                int userId = UserRepository.CreateUser(user);
                if (context.IsPartiallyAuthenticated == true)
                {
                    return RedirectToAction("SignupComplete", "Account");
                }
                else
                {
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

                    if (requireEmailVerification)
                    {
                        return RedirectToAction("SignupPendingVerification", "Account");
                    }
                    else if (requestEmailVerification)
                    {
                        return RedirectToAction("SignupCompleteVerification", "Account");
                    }
                    else
                    {
                        return RedirectToAction("SignupComplete", "Account");
                    }
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
            ViewBag.Context.Title = $"Confirm";
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
                model.SuccessMessage = "Your account has been confirmed, feel free to login.";

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
            ViewBag.Context.Title = $"Avatar";
            if (context.CanView == false)
            {
                return Unauthorized();
            }

            userAccountName = WikiUtility.CleanPartialURI(userAccountName);
            string scale = Request.Query["Scale"].ToString().ToString().IsNullOrEmpty("100");
            string max = Request.Query["max"];
            string exact = Request.Query["exact"];

            if (string.IsNullOrWhiteSpace(max))
            {
                max = "512"; //A safe default?
            }

            byte[] imageBytes = UserRepository.GetUserAvatarByNavigation(userAccountName);

            if (imageBytes == null || imageBytes.Count() == 0)
            {
                //Load the default avatar.
                var image = Image.Load("Avatar.png");
                using var ms = new MemoryStream();
                image.SaveAsPng(ms);
                imageBytes = ms.ToArray();
            }

            if (imageBytes != null && imageBytes.Count() > 0)
            {
                var img = Image.Load(new MemoryStream(imageBytes));

                int iScale = int.Parse(scale);
                int iMax = int.Parse(max);

                if (string.IsNullOrEmpty(exact) == false)
                {
                    int iexact = int.Parse(exact);
                    if (iexact > 1024)
                    {
                        iexact = 1024;
                    }
                    else if (iexact < 16)
                    {
                        iexact = 16;
                    }

                    int diff = img.Width - iexact;
                    int width = (int)(img.Width - diff);
                    int height = (int)(img.Height - diff);

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  deminsion to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both demensions.
                    if (height < 16)
                    {
                        int difference = 16 - height;
                        height += difference;
                        width += difference;
                    }
                    if (width < 16)
                    {
                        int difference = 16 - width;
                        height += difference;
                        width += difference;
                    }

                    using var image = Images.ResizeImage(img, width, height);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
                }
                else if (iMax != 0 && (img.Width > iMax || img.Height > iMax))
                {
                    int diff = img.Width - iMax;
                    int width = (int)(img.Width - diff);
                    int height = (int)(img.Height - diff);

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  deminsion to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both demensions.
                    if (height < 16)
                    {
                        int difference = 16 - height;
                        height += difference;
                        width += difference;
                    }
                    if (width < 16)
                    {
                        int difference = 16 - width;
                        height += difference;
                        width += difference;
                    }

                    using var image = Images.ResizeImage(img, width, height);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");

                }
                else if (iScale != 100)
                {
                    int width = (int)(img.Width * (iScale / 100.0));
                    int height = (int)(img.Height * (iScale / 100.0));

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  deminsion to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both demensions.
                    if (height < 16)
                    {
                        int difference = 16 - height;
                        height += difference;
                        width += difference;
                    }
                    if (width < 16)
                    {
                        int difference = 16 - width;
                        height += difference;
                        width += difference;
                    }

                    using var image = Images.ResizeImage(img, width, height);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
                }
                else
                {
                    using var ms = new MemoryStream();
                    img.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
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
            ViewBag.Context.Title = $"Profile";
            var user = UserRepository.GetUserById(context.User.Id);

            var model = new UserProfileModel()
            {
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
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
            ViewBag.Context.Title = $"Profile";
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();

            model.Navigation = WikiUtility.CleanPartialURI(model.AccountName.ToLower());
            var user = UserRepository.GetUserById(context.User.Id);

            var file = Request.Form.Files["Avatar"];
            if (file != null && file.Length > 0)
            {
                try
                {
                    var imageBytes = Utility.ConvertHttpFileToBytes(file);
                    //This is just to ensure this is a valid image:
                    var image = Image.Load(new MemoryStream(imageBytes));
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
                user.Role = user.Role; //We don't allow this to be changed here.
                user.AccountName = model.AccountName;
                user.Navigation = WikiUtility.CleanPartialURI(model.AccountName);
                user.EmailAddress = model.EmailAddress;
                user.ModifiedDate = DateTime.UtcNow;
                UserRepository.UpdateUser(user);

                model.SuccessMessage = "Your profile has been saved successfully!.";
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Delete(AccountModel model)
        {
            if (context.CanDelete == false)
            {
                return Unauthorized();
            }

            var user = UserRepository.GetUserById(context.User.Id);

            bool confirmAction = bool.Parse(Request.Form["Action"]);
            if (confirmAction == true && user != null)
            {
                UserRepository.DeleteById(user.Id);
                Cache.ClearClass($"User:{user.Navigation}");

                HttpContext.SignOutAsync();
                return RedirectToAction("Display", "Page", "Home");
            }

            return RedirectToAction("UserProfile", "Account");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Delete()
        {
            if (context.CanDelete == false)
            {
                return Unauthorized();
            }

            var user = UserRepository.GetUserById(context.User.Id);

            ViewBag.AccountName = user.AccountName;

            var model = new AccountModel()
            {
            };

            if (user != null)
            {
                ViewBag.Context.Title = $"{user.AccountName} Delete";
            }

            return View(model);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using SharpWiki.Shared.Library;
using SharpWiki.Shared.Models.View;
using SharpWiki.Shared.Repository;
using SharpWiki.Shared.Wiki;

namespace SharpWikiCom.Controllers
{
    [Authorize]
    public class UserController : ControllerHelperBase
    {
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
                    PerformLogin(user.EmailAddress, user.Password);

                    if (Request.QueryString["ReturnUrl"] != null && Request.QueryString["ReturnUrl"] != "/")
                    {
                        return Redirect(Request.QueryString["ReturnUrl"]);
                    }
                    else
                    {
                        return RedirectToAction("Content", "Wiki", "Home");
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

        /// <summary>
        /// Populate reset password form.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Reset()
        {
            return View();
        }

        /// <summary>
        /// Populate signup page.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Signup()
        {
            if (ConfigurationRepository.Get("Membership", "Allow Signup", false) == false)
            {
                return new HttpUnauthorizedResult();
            }

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
        public ActionResult Signup(SignupModel user)
        {
            if (ConfigurationRepository.Get("Membership", "Allow Signup", false) == false)
            {
                return new HttpUnauthorizedResult();
            }


            /*
                    string newEmailAddress = Utility.StripHTML(Request.Form["EmailAddress"]);
                    string newDisplayName = Utility.StripHTML(Request.Form["DisplayName"]);

                    if (Currentuser().EmailAddress.ToLower() != newEmailAddress.ToLower())
                    {
                        if ((newEmailAddress?.Length ?? 0) < 5)
                        {
                            ViewBag.Validation = Utility.ValidationMessage("You must enter an email address");
                            return View();
                        }
                        else if (Utility.IsValidEmail(newEmailAddress) == false)
                        {
                            ViewBag.Validation = Utility.ValidationMessage("You'll need to specifiy a valid(ish) email address");
                            return View();
                        }
                        else if (UserRepository.DoesEmailAddressExist(newEmailAddress))
                        {
                            ViewBag.Validation = Utility.ValidationMessage("This email address is already in use", "If you have forgotten your password, reset it");
                            return View();
                        }
                    }
                    else if (Currentuser().DisplayName.ToLower() != newDisplayName.ToLower())
                    {
                        if ((newDisplayName?.Length ?? 0) < 2)
                        {
                            ViewBag.Validation = Utility.ValidationMessage("You must enter a display name / alias");
                            return View();
                        }
                        else if (UserRepository.DoesDisplayNameExist(newDisplayName))
                        {
                            ViewBag.Validation = Utility.ValidationMessage("This display name is already in use", "If you have forgotten your password, reset it", "Otherwise, be more creative");
                            return View();
                        }
                    }

                    UserRepository.UpdateUser(Currentuser().Id, newEmailAddress, newDisplayName);

                    (Session["LoggedInSession"] as LoggedInSession).User = UserRepository.GetUserById(Currentuser().Id);

                    LoadSessionGlobals();
                }
                else if (formType == "Password")
                {
                    string newPassword = Request.Form["Password"];
                    string newPassword2 = Request.Form["Password2"];

                    if ((newPassword?.Length ?? 0) < 5 != (newPassword2?.Length ?? 0) < 5)
                    {
                        ViewBag.Validation = Utility.ValidationMessage("The password is too short. 5 character minimum.");
                        return View();
                    }
                    else if (newPassword != newPassword2)
                    {
                        ViewBag.Validation = Utility.ValidationMessage("The passwords you entered do not match");
                        return View();
                    }

                    UserRepository.UpdateUserPassword(Currentuser().Id, newPassword);
                }
            */

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
        public ActionResult UserProfile([Bind(Exclude = "Avatar")] UserProfileModel profile)
        {
            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();

            if (ModelState.IsValid)
            {
                profile.Navigation = WikiUtility.CleanPartialURI(profile.AccountName.ToLower());

                var user = UserRepository.GetUserById(context.User.Id);

                if (user.Navigation != profile.Navigation)
                {
                    var checkName = UserRepository.GetUserByNavigation(WikiUtility.CleanPartialURI(profile.AccountName.ToLower()));
                    if (checkName != null)
                    {
                        ModelState.AddModelError("AccountName", "Account name is already in use.");
                        return View(profile);
                    }
                }

                if (user.EmailAddress.ToLower() != profile.EmailAddress.ToLower())
                {
                    var checkName = UserRepository.GetUserByEmail(profile.EmailAddress.ToLower());
                    if (checkName != null)
                    {
                        ModelState.AddModelError("EmailAddress", "Email address is already in use.");
                        return View(profile);
                    }
                }

                user.AboutMe = profile.AboutMe;
                user.FirstName = profile.FirstName;
                user.LastName = profile.LastName;
                user.TimeZone = profile.TimeZone;
                user.Country = profile.Country;
                user.AccountName = profile.AccountName;
                user.Navigation = WikiUtility.CleanPartialURI(profile.AccountName);
                user.EmailAddress = profile.EmailAddress;
                user.ModifiedDate = DateTime.UtcNow;
                UserRepository.UpdateUser(user);

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

                ViewBag.Success = "Your profile has been saved successfully!.";
            }

            return View(profile);
        }
    }
}

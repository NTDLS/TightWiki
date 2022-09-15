using System;
using System.Text.RegularExpressions;

namespace AsapWiki.Shared.Misc
{
    public static class Misc
    {
        public static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false;
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }

        public static string StripHTML(string input)
        {
            return Regex.Replace(input ?? "", "<.*?>", String.Empty);
        }
    }
}

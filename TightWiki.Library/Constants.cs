namespace TightWiki.Library
{
    public static class Constants
    {
        public const string TagStart = @"Tw|";
        public const string TagEnd = @"|wT";

        public const string SoftBreak = "Tw|SoftBreak|wT"; //These will remain as \r\n in the final HTML.
        public const string HardBreak = "Tw|HardBreak|wT"; //These will remain as <br /> in the final HTML.

        public const string CRYPTOCHECK = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string DEFAULTUSERNAME = "admin@tightwiki.com";
        public const string DEFAULTACCOUNT = "admin";
        public const string DEFAULTPASSWORD = "2Tight2Wiki@";
    }
}

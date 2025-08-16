namespace TightWiki.Library
{
    public static class Constants
    {
        public const string CRYPTOCHECK = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string DEFAULTUSERNAME = "admin@tightwiki.com";
        public const string DEFAULTACCOUNT = "admin";
        public const string DEFAULTPASSWORD = "2Tight2Wiki@";

        public enum WikiTheme
        {
            Light,
            Dark
        }

        public enum AdminPasswordChangeState
        {
            /// <summary>
            /// The password has not been changed, display a big warning.
            /// </summary>
            IsDefault,
            /// <summary>
            /// All is well!
            /// </summary>
            HasBeenChanged,
            /// <summary>
            /// The default password status does not exist and the password needs to be set to default.
            /// </summary>
            NeedsToBeSet
        }

        public static class WikiInstruction
        {
            public static string Deprecate { get; } = "Deprecate";
            public static string Protect { get; } = "Protect";
            public static string Template { get; } = "Template";
            public static string Review { get; } = "Review";
            public static string Include { get; } = "Include";
            public static string Draft { get; } = "Draft";
            public static string NoCache { get; } = "NoCache";
            public static string HideFooterComments { get; } = "HideFooterComments";
            public static string HideFooterLastModified { get; } = "HideFooterLastModified";
        }

        public static class ConfigurationGroup
        {
            public const string Basic = "Basic";
            public const string Cookies = "Cookies";
            public const string Search = "Search";
            public const string Functionality = "Functionality";
            public const string Membership = "Membership";
            public const string Email = "Email";
            public const string HTMLLayout = "HTML Layout";
            public const string Performance = "Performance";
            public const string Customization = "Customization";
            public const string ExternalAuthentication = "External Authentication";
            public const string FilesAndAttachments = "Files and Attachments";

        }
    }
}

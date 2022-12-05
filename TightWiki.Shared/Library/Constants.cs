namespace TightWiki.Shared.Library
{
    public static class Constants
    {
        public const string CRYPTOCHECK = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string DEFAULTPASSWORD = "2Tight2Wiki!";

        public static class WikiInstruction
        {
            public static string Deprecate { get; } = "Deprecate";
            public static string Protect { get; } = "Protect";
            public static string Template { get; } = "Template";
            public static string Review { get; } = "Review";
            public static string Include { get; } = "Include";
            public static string Draft { get; } = "Draft";
        }

        public static class Roles
        {
            /// <summary>
            /// Administrators can do anything. Add, edit, delete, pages, users, etc.
            /// </summary>
            public static string Administrator = "Administrator";
            /// <summary>
            /// Read-only user with a profile.
            /// </summary>
            public static string Member = "Member";
            /// <summary>
            /// Contributor can add and edit pages.
            /// </summary>
            public static string Contributor = "Contributor";
            /// <summary>
            /// Moderators can add, edit and delete pages.
            /// </summary>
            public static string Moderator = "Moderator";
        }
    }
}

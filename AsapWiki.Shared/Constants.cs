using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared
{
    public static class Constants
    {
        public enum CacheCategory
        {
            Page
        }

        public static class WikiInstruction
        {
            public static string Depreciate = "Depreciate";
            public static string Protect = "Protect";
            public static string Template = "Template";
            public static string Review = "Review";
            public static string Include = "Include";
            public static string Draft = "Draft";
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

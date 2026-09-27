using TightWiki.Plugin.Models;

namespace TightWiki.Library.Security
{
    /// <summary>
    /// Evaluates a user's apparent permissions against a page or namespace.
    /// </summary>
    public static class TwPermissionEvaluator
    {
        private static readonly string _denyString = Plugin.TwPermissionDisposition.Deny.ToString();
        private static readonly string _allowString = Plugin.TwPermissionDisposition.Allow.ToString();

        /// <summary>
        /// Returns true if the user holds ANY one of the given permissions. Each permission is evaluated individually:
        ///  an explicit allow grants access, while a deny or no matching rule moves on to the next permission.
        ///  Page rules are not considered for Create, because Create is about a page that does not exist yet,
        ///  but namespace rules are, so that creating pages can be allowed or denied per namespace.
        /// </summary>
        public static bool HoldsAny(IEnumerable<TwApparentPermission> permissions,
            Plugin.TwPermission[] requested, TwPage? page, string? inferredNamespace = null)
        {
            foreach (var permission in requested)
            {
                if (permission == Plugin.TwPermission.Create)
                {
                    //Drop the page, but keep its namespace (or the one parsed from the URL when the page does not exist).
                    var createNamespace = page != null
                        ? (string.IsNullOrEmpty(page.Namespace) ? null : page.Namespace)
                        : inferredNamespace;

                    if (Evaluate(permissions, permission, null, createNamespace) == true)
                    {
                        return true;
                    }
                }
                else if (Evaluate(permissions, permission, page, inferredNamespace) == true)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the permission is explicitly allowed, false if it is explicitly denied, or null if
        /// no rule applies. Rules are evaluated from most to least specific, and the first match wins:
        ///  the page, the page's namespace, all pages (*), then all namespaces (*). Within each level a deny
        ///  is checked before an allow, so a specific deny overrides a wildcard allow.
        /// </summary>
        /// <param name="permissions">The user's apparent permissions.</param>
        /// <param name="permission">The permission being evaluated.</param>
        /// <param name="page">The page being accessed, if it exists.</param>
        /// <param name="inferredNamespace">The namespace parsed from the requested URL, used when the page does not exist
        /// so that namespace rules are still honoured.</param>
        public static bool? Evaluate(IEnumerable<TwApparentPermission> permissions,
            Plugin.TwPermission permission, TwPage? page, string? inferredNamespace = null)
        {
            string permissionString = permission.ToString();

            // Resolve the namespace to check: prefer the actual page's namespace, fall back to what was
            // parsed from the URL so that deny rules are honoured even for non-existent pages.
            string? effectiveNamespace = page != null
                ? (string.IsNullOrEmpty(page.Namespace) ? null : page.Namespace)
                : inferredNamespace;

            bool Any(Func<TwApparentPermission, bool> scope, string disposition)
                => permissions.Any(o => scope(o)
                    && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                    && o.PermissionDisposition.Equals(disposition, StringComparison.InvariantCultureIgnoreCase));

            if (page != null)
            {
                var pageIdString = page.Id.ToString();

                if (Any(o => o.PageId == pageIdString, _denyString)) return false;
                if (Any(o => o.PageId == pageIdString, _allowString)) return true;
            }

            if (effectiveNamespace != null)
            {
                bool isNamespace(TwApparentPermission o)
                    => o.Namespace?.Equals(effectiveNamespace, StringComparison.InvariantCultureIgnoreCase) == true;

                if (Any(isNamespace, _denyString)) return false;
                if (Any(isNamespace, _allowString)) return true;
            }

            static bool isAllPages(TwApparentPermission o) => o.PageId?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true;
            if (Any(isAllPages, _denyString)) return false;
            if (Any(isAllPages, _allowString)) return true;

            static bool isAllNamespaces(TwApparentPermission o) => o.Namespace?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true;
            if (Any(isAllNamespaces, _denyString)) return false;
            if (Any(isAllNamespaces, _allowString)) return true;

            return null;
        }
    }
}

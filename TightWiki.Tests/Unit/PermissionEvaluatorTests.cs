using TightWiki.Library.Security;
using TightWiki.Plugin.Models;
using TwPermission = TightWiki.Plugin.TwPermission;

namespace TightWiki.Tests.Unit
{
    public class PermissionEvaluatorTests
    {
        private static readonly TwPage SecurePage = new() { Id = 42, Name = "Secure :: Payroll" };
        private static readonly TwPage PublicPage = new() { Id = 7, Name = "Public :: Welcome" };
        private static readonly TwPage RootPage = new() { Id = 1, Name = "Home" };

        private static TwApparentPermission Rule(string disposition, TwPermission permission, string? ns = null, string? pageId = null)
            => new() { PermissionDisposition = disposition, Permission = permission.ToString(), Namespace = ns, PageId = pageId };

        private static TwApparentPermission Allow(TwPermission permission, string? ns = null, string? pageId = null)
            => Rule("Allow", permission, ns, pageId);

        private static TwApparentPermission Deny(TwPermission permission, string? ns = null, string? pageId = null)
            => Rule("Deny", permission, ns, pageId);

        [Fact]
        public void NoRulesIsUndecided()
        {
            Assert.Null(TwPermissionEvaluator.Evaluate([], TwPermission.Read, PublicPage));
        }

        [Fact]
        public void WildcardNamespaceAllowGrantsEveryNamespace()
        {
            List<TwApparentPermission> rules = [Allow(TwPermission.Read, ns: "*")];

            Assert.True(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, PublicPage));
            Assert.True(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, SecurePage));
            Assert.True(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, RootPage));
        }

        [Fact]
        public void SpecificNamespaceDenyOverridesWildcardAllow()
        {
            //https://github.com/NTDLS/TightWiki/issues/96 - read access to everything except the Secure namespace.
            List<TwApparentPermission> rules = [Allow(TwPermission.Read, ns: "*"), Deny(TwPermission.Read, ns: "Secure")];

            Assert.False(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, SecurePage));
            Assert.True(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, PublicPage));
            Assert.True(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, RootPage));
        }

        [Fact]
        public void NamespaceDenyAppliesToPagesThatDoNotExist()
        {
            List<TwApparentPermission> rules = [Allow(TwPermission.Read, ns: "*"), Deny(TwPermission.Read, ns: "Secure")];

            Assert.False(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, page: null, inferredNamespace: "Secure"));
            Assert.True(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, page: null, inferredNamespace: "Public"));
        }

        [Fact]
        public void NamespaceMatchingIsCaseInsensitive()
        {
            List<TwApparentPermission> rules = [Allow(TwPermission.Read, ns: "*"), Deny(TwPermission.Read, ns: "SECURE")];

            Assert.False(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, SecurePage));
        }

        [Fact]
        public void PermissionAndDispositionMatchingIsCaseInsensitive()
        {
            List<TwApparentPermission> rules = [new() { PermissionDisposition = "allow", Permission = "read", Namespace = "*" }];

            Assert.True(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, PublicPage));
        }

        [Fact]
        public void PageAllowOverridesNamespaceDeny()
        {
            List<TwApparentPermission> rules = [Deny(TwPermission.Read, ns: "Secure"), Allow(TwPermission.Read, pageId: "42")];

            Assert.True(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, SecurePage));
        }

        [Fact]
        public void PageDenyOverridesNamespaceAllow()
        {
            List<TwApparentPermission> rules = [Allow(TwPermission.Read, ns: "Secure"), Deny(TwPermission.Read, pageId: "42")];

            Assert.False(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, SecurePage));
        }

        [Fact]
        public void DenyWinsOverAllowAtTheSameLevel()
        {
            List<TwApparentPermission> rules = [Allow(TwPermission.Read, ns: "Secure"), Deny(TwPermission.Read, ns: "Secure")];

            Assert.False(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, SecurePage));
        }

        [Fact]
        public void AllPagesRulesAreCheckedBeforeAllNamespacesRules()
        {
            List<TwApparentPermission> rules = [Allow(TwPermission.Read, ns: "*"), Deny(TwPermission.Read, pageId: "*")];

            Assert.False(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, PublicPage));
        }

        [Fact]
        public void RulesForOtherPermissionsAreIgnored()
        {
            List<TwApparentPermission> rules = [Allow(TwPermission.Edit, ns: "*"), Deny(TwPermission.Delete, ns: "*")];

            Assert.Null(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, PublicPage));
        }

        [Fact]
        public void RootNamespacePageIsNotAffectedByNamedNamespaceRules()
        {
            List<TwApparentPermission> rules = [Deny(TwPermission.Read, ns: "Secure")];

            Assert.Null(TwPermissionEvaluator.Evaluate(rules, TwPermission.Read, RootPage));
        }

        [Fact]
        public void HoldsAnyGrantsWhenAnyRequestedPermissionIsAllowed()
        {
            List<TwApparentPermission> rules = [Deny(TwPermission.Edit, ns: "Secure"), Allow(TwPermission.Moderate, ns: "*")];

            Assert.True(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Edit, TwPermission.Moderate], SecurePage));
            Assert.False(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Edit], SecurePage));
        }

        [Fact]
        public void HoldsAnyTreatsUndecidedAsNotHeld()
        {
            Assert.False(TwPermissionEvaluator.HoldsAny([], [TwPermission.Read], PublicPage));
        }

        [Fact]
        public void HoldsAnyIgnoresPageRulesForCreate()
        {
            //Create is about a page that does not exist yet, so rules on an existing page id do not apply to it.
            List<TwApparentPermission> rules = [Allow(TwPermission.Create, pageId: "*"), Deny(TwPermission.Create, pageId: "42")];

            Assert.True(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Create], SecurePage));
        }

        [Fact]
        public void CreateHonoursNamespaceDenyForNewPages()
        {
            //Creating "Secure :: Anything" must be refused when Create is denied in Secure, even with Create allowed everywhere.
            List<TwApparentPermission> rules = [Allow(TwPermission.Create, ns: "*"), Deny(TwPermission.Create, ns: "Secure")];

            Assert.False(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Create], page: null, inferredNamespace: "Secure"));
            Assert.True(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Create], page: null, inferredNamespace: "Public"));
            Assert.True(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Create], page: null, inferredNamespace: null));
        }

        [Fact]
        public void CreateCanBeGrantedForASingleNamespace()
        {
            List<TwApparentPermission> rules = [Allow(TwPermission.Create, ns: "Sandbox")];

            Assert.True(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Create], page: null, inferredNamespace: "Sandbox"));
            Assert.False(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Create], page: null, inferredNamespace: "Public"));
            Assert.False(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Create], page: null, inferredNamespace: null));
        }

        [Fact]
        public void CreateFromAnExistingPageUsesThatPagesNamespace()
        {
            //The "Create" button on an existing page asks whether pages can be created in that page's namespace.
            List<TwApparentPermission> rules = [Allow(TwPermission.Create, ns: "*"), Deny(TwPermission.Create, ns: "Secure")];

            Assert.False(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Create], SecurePage));
            Assert.True(TwPermissionEvaluator.HoldsAny(rules, [TwPermission.Create], PublicPage));
        }
    }
}

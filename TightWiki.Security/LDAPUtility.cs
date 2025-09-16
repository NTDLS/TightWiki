using Microsoft.Extensions.Localization;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.Protocols;
using System.Net;
using TightWiki.Models.DataModels;

namespace TightWiki.Security
{
    public class LDAPUtility
    {
        private static string GetLdapSearchBase(string domainName)
        {
            // domainName like "corp.example.com"
            var parts = domainName.Split('.');
            return string.Join(",", parts.Select(p => $"DC={p}"));
        }

        private static string EscapeLdapFilterValue(string value)
        {
            // RFC 4515 escaping: \ * ( ) and NUL
            return value
                .Replace(@"\", @"\5c")
                .Replace("*", @"\2a")
                .Replace("(", @"\28")
                .Replace(")", @"\29")
                .Replace("\0", @"\00");
        }

        private static (string? domain, string sam, string? upn) ParseLogin(string defaultSignInDomain, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(nameof(input));

            //DOMAIN\user
            var slash = input.IndexOf('\\');
            if (slash >= 0)
            {
                var domain = input.Substring(0, slash);
                var user = input.Substring(slash + 1);
                return (domain, user, null);
            }

            //user@domain.com (UPN)
            var at = input.IndexOf('@');
            if (at >= 0)
            {
                return (string.IsNullOrEmpty(defaultSignInDomain) ? null : defaultSignInDomain, input.Substring(0, at), input); // upn = full input
            }

            //plain sAMAccountName
            return (string.IsNullOrEmpty(defaultSignInDomain) ? null : defaultSignInDomain, input, null);
        }

        public static bool LdapCredentialChallenge(ConfigurationEntries externalAuthentication, IStringLocalizer localizer,
            string username, string password, [NotNullWhen(true)] out string? samAccountName, [NotNullWhen(true)] out Guid? objectGuid)
        {
            var useSecureSocketLayer = externalAuthentication.Value("LDAP: Use Secure Socket Layer", false);
            var EnableLDAPAuthentication = externalAuthentication.Value("LDAP: Enable LDAP Authentication", false);
            var fullyQualifiedDomain = externalAuthentication.Value("LDAP: Fully-Qualified Domain", string.Empty);
            var defaultSignInDomain = externalAuthentication.Value("LDAP: Default Sign-in Domain", string.Empty);

            var (domain, sam, upn) = ParseLogin(defaultSignInDomain, username);

            if (string.IsNullOrEmpty(upn) == false)
            {
                username = upn; //Prefer UPN if we have it.
            }
            else if (string.IsNullOrEmpty(sam) == false)
            {
                if (string.IsNullOrEmpty(domain) == false)
                {
                    username = $"{sam}@{domain}";
                }
                else
                {
                    username = sam;
                }
            }
            else
            {
                throw new Exception(localizer["Either SAM or UPN are required for LDAP login."]);
            }

            using var conn = new LdapConnection(fullyQualifiedDomain);
            conn.SessionOptions.SecureSocketLayer = useSecureSocketLayer;
            conn.AuthType = AuthType.Basic;

            conn.Bind(new NetworkCredential(username, password));

            var ldapSearchBase = GetLdapSearchBase(fullyQualifiedDomain);

            //Always try sAMAccountName=sam
            var clauses = new List<string> { $"(sAMAccountName={EscapeLdapFilterValue(sam)})" };

            //If we actually received a UPN, also try userPrincipalName
            if (!string.IsNullOrEmpty(upn))
                clauses.Add($"(userPrincipalName={EscapeLdapFilterValue(upn)})");

            if (domain != null)
                clauses.Add($"(userPrincipalName={EscapeLdapFilterValue($"{sam}@{domain}")})");

            var filter = $"(|{string.Join("", clauses)})";

            var request = new SearchRequest(
                ldapSearchBase,
                filter,
                SearchScope.Subtree,
                "sAMAccountName", "userPrincipalName", "cn", "mail", "distinguishedName", "objectGUID"
            );

            var response = (SearchResponse)conn.SendRequest(request);

            if (response.Entries.Count > 0)
            {
                var entry = response.Entries[0];

                string displayName = entry.Attributes["displayName"]?[0]?.ToString()
                                     ?? entry.Attributes["cn"]?[0]?.ToString()
                                     ?? sam;

                samAccountName = entry.Attributes["sAMAccountName"][0]?.ToString();
                objectGuid = new Guid((byte[])entry.Attributes["objectGUID"][0]);
                return samAccountName != null;
            }

            objectGuid = null;
            samAccountName = null;
            return false;
        }
    }
}

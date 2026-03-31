namespace TightWiki.Plugin
{
    public static class TwConstants
    {
        public const string SoftBreak = "<!--SoftBreak-->"; //These will remain as \r\n in the final HTML.
        public const string HardBreak = "<!--HardBreak-->"; //These will remain as <br /> in the final HTML.

        public const string CRYPTOCHECK = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string DEFAULTUSERNAME = "admin@tightwiki.com";
        public const string DEFAULTACCOUNT = "admin";
        public const string DEFAULTPASSWORD = "2Tight2Wiki@";

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

        public static class WikiConfigurationGroup
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
            public const string LDAPAuthentication = "LDAP Authentication";
            public const string FilesAndAttachments = "Files and Attachments";
        }
    }

    public enum WikiDefaultDataType
    {
        Configurations,
        Themes,
        WikiHelpPages,
        WikiBuiltinPages,
        WikiIncludePages,
        FeatureTemplates = 5
    }

    public enum WikiPermissionDisposition
    {
        Allow,
        Deny
    }

    public enum WikiRoles
    {
        Administrator,
        Member,
        Contributor,
        Moderator,
        Anonymous
    }

    public enum WikiPermission
    {
        Read,
        Edit,
        Delete,
        Moderate,
        Create
    }

    public enum WikiTheme
    {
        Light,
        Dark
    }

    public enum WikiAdminPasswordChangeState
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


    public enum WikiFunctionType
    {
        Standard,
        Scoped,
        Instruction
    }

    public enum WikiMatchType
    {
        ScopeFunction,
        Emoji,
        Instruction,
        Comment,
        Variable,
        Markup,
        Error,
        StandardFunction,
        Link,
        Heading,
        Literal
    }

    public enum HandlerResultInstruction
    {
        /// <summary>
        /// Does not process the match, allowing it to be processed by another handler.
        /// </summary>
        Skip,
        /// <summary>
        /// Removes any single trailing newline after match.
        /// </summary>
        TruncateTrailingLine,
        /// <summary>
        /// Will not continue to process content in this block.
        /// </summary>
        DisallowNestedProcessing,
        /// <summary>
        /// As opposed to the default functionality of replacing all matches, this will cause ony the first match to be replaced.
        /// This also means that each match will be processed individually, which can impact performance.
        /// </summary>
        OnlyReplaceFirstMatch
    }

    public enum TightWikiAlignStyle
    {
        Default,
        Start,
        Center,
        End
    }

    public enum TightWikiBootstrapStyle
    {
        Default,
        Primary,
        Secondary,
        Light,
        Dark,
        Success,
        Info,
        Warning,
        Danger,
        Muted,
        White
    }

    public enum TightWikiOrder
    {
        Ascending,
        Descending,
    }

    public enum TightWikiBulletStyle
    {
        Ordered,
        Unordered
    }

    public enum TightWikiListStyle
    {
        Full,
        List
    }

    public enum TightWikiTabularStyle
    {
        Full,
        List,
        Flat
    }

    public enum TightWikiLinkStyle
    {
        Text,
        Link,
        LinkName
    }

    public enum TightWikiCodeLanguage
    {
        Auto,
        Wiki,
        Cpp,
        Lua,
        GraphQL,
        Swift,
        R,
        Yaml,
        Kotlin,
        Scss,
        Shell,
        Vbnet,
        Json,
        ObjectiveC,
        Perl,
        Diff,
        Wasm,
        PHP,
        Xml,
        Bash,
        CSharp,
        CSS,
        GO,
        Ini,
        JavaScript,
        Less,
        Makefile,
        Markdown,
        PlainText,
        Python,
        Ruby,
        Rust,
        Sql,
        Typescript
    }
}

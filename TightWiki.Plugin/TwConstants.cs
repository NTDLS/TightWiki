namespace TightWiki.Plugin
{
    /// <summary>
    /// Provides constant values used throughout the application.
    /// </summary>
    public static class TwConstants
    {
        /// <summary>
        /// Page instructions that determine how a page is processed.
        /// </summary>
        public static class TwInstruction
        {
            /// <summary>
            /// The page will be deprecated and a warning message will be shown at the top of the page.
            /// This is intended to be used when a page is no longer relevant or accurate, but should
            /// not be deleted because it may still contain useful information or links.
            /// </summary>
            public static string Deprecate { get; } = "Deprecate";
            /// <summary>
            /// The pages is protected and cannot be edited by users who do not have admin permissions.
            /// This is intended to be used for pages that are critical to the wiki's operation or contain
            /// sensitive information, and should only be edited by administrators.
            /// </summary>
            public static string Protect { get; } = "Protect";
            /// <summary>
            /// The page is a template and should not be shown in search results or page lists.
            /// This is intended to be used for pages that are designed to be used as templates
            /// for other pages, and should not be treated as regular content pages.
            /// </summary>
            public static string Template { get; } = "Template";
            /// <summary>
            /// The page has been marked for review and a message will be shown at the top of the page indicating that it is under review.
            /// </summary>
            public static string Review { get; } = "Review";
            /// <summary>
            /// The page is an include page, meaning that it is intended to be included in other pages rather than viewed on its own.
            /// </summary>
            public static string Include { get; } = "Include";
            /// <summary>
            /// The page is still in draft form and should not be considered complete.
            /// A message will be shown at the top of the page indicating that it is a draft.
            /// </summary>
            public static string Draft { get; } = "Draft";
            /// <summary>
            /// The page should not be cached, and will be re-rendered on each request.
            /// This is intended to be used for pages that contain frequently changing
            /// information or dynamic content that should always be up to date.
            /// </summary>
            public static string NoCache { get; } = "NoCache";
            /// <summary>
            /// Indicates that the comment section should not be displayed in the page footer.
            /// </summary>
            public static string HideFooterComments { get; } = "HideFooterComments";
            /// <summary>
            /// Indicates that the last-modified-by and date should not be displayed in the page footer.
            /// </summary>
            public static string HideFooterLastModified { get; } = "HideFooterLastModified";
        }

        /// <summary>
        /// Configuration group fixed names.
        /// </summary>
        public static class TwConfigGroup
        {
            /// <summary>Basic configuration settings, such as site name and description.</summary>
            public const string Basic = "Basic";
            /// <summary>Configuration settings related to cookies, such as expiration and consent behavior.</summary>
            public const string Cookies = "Cookies";
            /// <summary>Configuration settings related to search behavior and indexing.</summary>
            public const string Search = "Search";
            /// <summary>Configuration settings related to site functionality and feature flags.</summary>
            public const string Functionality = "Functionality";
            /// <summary>Configuration settings related to user membership, registration, and permissions.</summary>
            public const string Membership = "Membership";
            /// <summary>Configuration settings related to email functionality, such as SMTP server settings and email templates.</summary>
            public const string Email = "Email";
            /// <summary>Configuration settings related to the HTML layout and structure of the wiki.</summary>
            public const string HTMLLayout = "HTML Layout";
            /// <summary>Configuration settings related to performance optimizations, such as caching and database tuning.</summary>
            public const string Performance = "Performance";
            /// <summary>Configuration settings related to site customization, such as themes and custom CSS.</summary>
            public const string Customization = "Customization";
            /// <summary>Configuration settings related to external authentication providers, such as OAuth and OpenID Connect.</summary>
            public const string ExternalAuthentication = "External Authentication";
            /// <summary>Configuration settings related to LDAP authentication, such as server URL and user search filters.</summary>
            public const string LDAPAuthentication = "LDAP Authentication";
            /// <summary>Configuration settings related to file storage, such as storage location, maximum file size, and allowed file types.</summary>
            public const string FilesAndAttachments = "Files and Attachments";
        }
    }

    /// <summary>
    /// Defines the various types of processing matches.
    /// </summary>
    public enum TwMatchType
    {
        /// <summary>Represents a processing error.</summary>
        Error,
        /// <summary>Match on a markup pattern.</summary>
        Markup,
        /// <summary>Match on a post-processing function.</summary>
        PostProcessingFunction,
        /// <summary>Match on a processing instruction.</summary>
        ProcessingInstruction,
        /// <summary>Match on a scope function.</summary>
        ScopeFunction,
        /// <summary>Match on a standard function.</summary>
        StandardFunction
    }

    /// <summary>
    /// Defines permission dispositions.
    /// </summary>
    public enum TwPermissionDisposition
    {
        /// <summary>The user is allowed to perform the action.</summary>
        Allow,
        /// <summary>The user is not allowed to perform the action.</summary>
        Deny
    }

    /// <summary>
    /// Defines special processing instructions that can be returned by functions to control how the content is processed.
    /// </summary>
    public enum TwResultInstruction
    {
        /// <summary>
        /// Does not process the match, allowing it to be processed by another handler.
        /// </summary>
        Skip,
        /// <summary>
        /// Removes any single trailing newline after the match.
        /// </summary>
        TruncateTrailingLine,
        /// <summary>
        /// Will not continue to process content in this block.
        /// </summary>
        DisallowNestedProcessing,
        /// <summary>
        /// As opposed to the default functionality of replacing all matches, this will cause only the first match to be replaced.
        /// This also means that each match will be processed individually, which can impact performance.
        /// </summary>
        OnlyReplaceFirstMatch
    }

    /// <summary>
    /// Bootstrap alignment styles.
    /// </summary>
    public enum TwAlignStyle
    {
        /// <summary>Default alignment, inherited from the parent element.</summary>
        Default,
        /// <summary>Aligns content to the start of the container (left for left-to-right languages).</summary>
        Start,
        /// <summary>Aligns content to the center of the container.</summary>
        Center,
        /// <summary>Aligns content to the end of the container (right for left-to-right languages).</summary>
        End
    }

    /// <summary>
    /// Bootstrap color styles.
    /// </summary>
    public enum TwBootstrapStyle
    {
        /// <summary>Default styling, typically unstyled or inherited from the parent element.</summary>
        Default,
        /// <summary>Primary action or brand color, used for main calls to action.</summary>
        Primary,
        /// <summary>Secondary action color, used for less prominent actions.</summary>
        Secondary,
        /// <summary>Light background style, typically a pale or white-toned variant.</summary>
        Light,
        /// <summary>Dark background style, typically a dark-toned variant.</summary>
        Dark,
        /// <summary>Indicates a successful or positive state.</summary>
        Success,
        /// <summary>Indicates an informational state.</summary>
        Info,
        /// <summary>Indicates a warning or cautionary state.</summary>
        Warning,
        /// <summary>Indicates a dangerous or error state.</summary>
        Danger,
        /// <summary>Muted or subdued text style, typically rendered in a lighter gray.</summary>
        Muted,
        /// <summary>White text or background style.</summary>
        White
    }

    /// <summary>
    /// Defines sort order directions.
    /// </summary>
    public enum TwOrder
    {
        /// <summary>Sorts items from lowest to highest.</summary>
        Ascending,
        /// <summary>Sorts items from highest to lowest.</summary>
        Descending,
    }

    /// <summary>
    /// Defines the bullet list style for rendered lists.
    /// </summary>
    public enum TwBulletStyle
    {
        /// <summary>An ordered (numbered) list.</summary>
        Ordered,
        /// <summary>An unordered (bulleted) list.</summary>
        Unordered
    }

    /// <summary>
    /// Defines the display style for list rendering.
    /// </summary>
    public enum TwListStyle
    {
        /// <summary>Renders the full list with all available detail.</summary>
        Full,
        /// <summary>Renders a compact list view.</summary>
        List
    }

    /// <summary>
    /// Defines the display style for tabular data rendering.
    /// </summary>
    public enum TwTabularStyle
    {
        /// <summary>Renders a full table with all columns and detail.</summary>
        Full,
        /// <summary>Renders a simplified list view of the data.</summary>
        List,
        /// <summary>Renders a flat, single-level table without grouping or nesting.</summary>
        Flat
    }

    /// <summary>
    /// Defines how links are rendered.
    /// </summary>
    public enum TwLinkStyle
    {
        /// <summary>Renders the link as plain text with no anchor.</summary>
        Text,
        /// <summary>Renders the link as a clickable hyperlink using the URL.</summary>
        Link,
        /// <summary>Renders the link as a clickable hyperlink using the link's display name.</summary>
        LinkName
    }

    /// <summary>
    /// Defines the syntax highlighting language for code blocks.
    /// </summary>
    public enum TwCodeLanguage
    {
        /// <summary>Automatically detects the language.</summary>
        Auto,
        /// <summary>TightWiki markup language.</summary>
        Wiki,
        /// <summary>C++ programming language.</summary>
        Cpp,
        /// <summary>Lua scripting language.</summary>
        Lua,
        /// <summary>GraphQL query language.</summary>
        GraphQL,
        /// <summary>Swift programming language.</summary>
        Swift,
        /// <summary>R statistical programming language.</summary>
        R,
        /// <summary>YAML data serialization format.</summary>
        Yaml,
        /// <summary>Kotlin programming language.</summary>
        Kotlin,
        /// <summary>SCSS stylesheet language.</summary>
        Scss,
        /// <summary>Shell scripting language.</summary>
        Shell,
        /// <summary>VB.NET programming language.</summary>
        Vbnet,
        /// <summary>JSON data format.</summary>
        Json,
        /// <summary>Objective-C programming language.</summary>
        ObjectiveC,
        /// <summary>Perl programming language.</summary>
        Perl,
        /// <summary>Diff/patch format.</summary>
        Diff,
        /// <summary>WebAssembly format.</summary>
        Wasm,
        /// <summary>PHP scripting language.</summary>
        PHP,
        /// <summary>XML markup language.</summary>
        Xml,
        /// <summary>Bash shell scripting language.</summary>
        Bash,
        /// <summary>C# programming language.</summary>
        CSharp,
        /// <summary>Cascading Style Sheets.</summary>
        CSS,
        /// <summary>Go programming language.</summary>
        GO,
        /// <summary>INI configuration file format.</summary>
        Ini,
        /// <summary>JavaScript programming language.</summary>
        JavaScript,
        /// <summary>Less stylesheet language.</summary>
        Less,
        /// <summary>Makefile build automation format.</summary>
        Makefile,
        /// <summary>Markdown lightweight markup language.</summary>
        Markdown,
        /// <summary>Plain unformatted text.</summary>
        PlainText,
        /// <summary>Python programming language.</summary>
        Python,
        /// <summary>Ruby programming language.</summary>
        Ruby,
        /// <summary>Rust programming language.</summary>
        Rust,
        /// <summary>SQL database query language.</summary>
        Sql,
        /// <summary>TypeScript programming language.</summary>
        Typescript
    }

    /// <summary>
    /// Defines the types of default data that can be seeded into the wiki.
    /// </summary>
    public enum TwDefaultDataType
    {
        /// <summary>Default configuration settings.</summary>
        Configurations,
        /// <summary>Default visual themes.</summary>
        Themes,
        /// <summary>Built-in help documentation pages.</summary>
        HelpPages,
        /// <summary>Core built-in wiki pages.</summary>
        BuiltinPages,
        /// <summary>Reusable include pages.</summary>
        IncludePages,
        /// <summary>Predefined feature templates.</summary>
        FeatureTemplates = 5
    }

    /// <summary>
    /// Defines the available user roles within the wiki.
    /// </summary>
    public enum TwRoles
    {
        /// <summary>Full administrative access to all wiki settings and content.</summary>
        Administrator,
        /// <summary>A registered member with standard access.</summary>
        Member,
        /// <summary>A user who can contribute and edit content.</summary>
        Contributor,
        /// <summary>A user with moderation privileges over content and users.</summary>
        Moderator,
        /// <summary>An unauthenticated visitor with read-only access.</summary>
        Anonymous
    }

    /// <summary>
    /// Defines the available content permissions.
    /// </summary>
    public enum TwPermission
    {
        /// <summary>Permission to read and view content.</summary>
        Read,
        /// <summary>Permission to edit existing content.</summary>
        Edit,
        /// <summary>Permission to delete content.</summary>
        Delete,
        /// <summary>Permission to moderate content and users.</summary>
        Moderate,
        /// <summary>Permission to create new content.</summary>
        Create
    }

    /// <summary>
    /// Defines the visual theme of the wiki.
    /// </summary>
    public enum TwTheme
    {
        /// <summary>Light color scheme.</summary>
        Light,
        /// <summary>Dark color scheme.</summary>
        Dark
    }

    /// <summary>
    /// Defines the state of the admin default password.
    /// </summary>
    public enum TwAdminPasswordChangeState
    {
        /// <summary>
        /// The password has not been changed from the default; a prominent warning will be displayed.
        /// </summary>
        IsDefault,
        /// <summary>
        /// The password has been changed from the default; all is well.
        /// </summary>
        HasBeenChanged,
        /// <summary>
        /// The default password status record does not exist and needs to be initialized.
        /// </summary>
        NeedsToBeSet
    }

    /// <summary>
    /// Defines the types of wiki functions.
    /// </summary>
    public enum TwFunctionType
    {
        /// <summary>A standard function that processes inline content.</summary>
        Standard,
        /// <summary>A scoped function that operates over a block of content.</summary>
        Scoped,
        /// <summary>A processing instruction that controls page-level behavior.</summary>
        Instruction
    }
}
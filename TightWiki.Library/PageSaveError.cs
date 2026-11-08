namespace TightWiki.Library
{
    /// <summary>
    /// The reason that a page was not saved.
    /// </summary>
    public enum PageSaveError
    {
        /// <summary>The page was saved.</summary>
        None,
        ChangeSummaryRequired,
        NameContainsUnsafeCharacters,
        NameContainsMultipleNamespaceSeparators,
        /// <summary>The session does not hold the Create permission for a new page.</summary>
        CreateNotPermitted,
        /// <summary>The session does not hold the Edit permission for an existing page.</summary>
        EditNotPermitted,
        /// <summary>The page is protected and the session does not hold the Moderate permission.</summary>
        PageProtected,
        /// <summary>A page with the (new) name already exists.</summary>
        NameAlreadyExists,
        /// <summary>The page was modified by someone else after the given revision.</summary>
        RevisionConflict
    }
}

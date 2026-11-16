namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Stores the content of page file attachments outside of the database. The database keeps the metadata
    /// (name, type, size, hash) and each file revision's content is kept here, identified by its page file id and revision.
    /// </summary>
    /// <remarks>Revisions are never modified once written: a changed file is stored as a new revision.
    /// Attachments uploaded before this storage existed still have their content in the database, so callers
    /// fall back to the database content when <see cref="Exists"/> returns false.</remarks>
    public interface ITwAttachmentStorage
    {
        /// <summary>
        /// The directory that attachment files are stored in.
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// Returns the full path of the file that holds the given page file revision, whether or not it exists.
        /// </summary>
        string GetPath(int pageFileId, int revision);

        /// <summary>
        /// Returns true if the content of the given page file revision is stored here.
        /// </summary>
        bool Exists(int pageFileId, int revision);

        /// <summary>
        /// Stores the content of a page file revision, replacing any file already at that location.
        /// The file only appears at its final location once it has been completely written.
        /// </summary>
        Task WriteAsync(int pageFileId, int revision, byte[] data);

        /// <summary>
        /// Returns the content of a page file revision, or null if it is not stored here.
        /// </summary>
        Task<byte[]?> ReadAllBytesAsync(int pageFileId, int revision);

        /// <summary>
        /// Deletes the content of a page file revision. Does nothing if it is not stored here.
        /// </summary>
        void Delete(int pageFileId, int revision);

        /// <summary>
        /// Renames the content of a page file revision to another revision of the same file, replacing
        /// anything already stored as the target revision. Does nothing if the source is not stored here.
        /// </summary>
        void MoveRevision(int pageFileId, int fromRevision, int toRevision);

        /// <summary>
        /// Returns the full path of every stored attachment file (uploads still being written are excluded).
        /// </summary>
        IEnumerable<string> EnumerateFiles();
    }
}

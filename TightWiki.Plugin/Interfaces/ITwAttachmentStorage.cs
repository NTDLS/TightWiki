namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Stores the content of page file attachments outside of the database. The database keeps the metadata
    /// (name, type, size, hash) and each file revision's content is kept here, identified by its page file id and revision
    /// and named with the extension of the file that was uploaded.
    /// </summary>
    /// <remarks>Revisions are never modified once written: a changed file is stored as a new revision.
    /// Attachments uploaded before this storage existed still have their content in the database, so callers
    /// fall back to the database content when <see cref="FindPath"/> returns null.</remarks>
    public interface ITwAttachmentStorage
    {
        /// <summary>
        /// The directory that attachment files are stored in.
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// Returns the full path of the file that holds the given page file revision, or null if it is not stored here.
        /// </summary>
        string? FindPath(int pageFileId, int revision);

        /// <summary>
        /// Returns true if the content of the given page file revision is stored here.
        /// </summary>
        bool Exists(int pageFileId, int revision);

        /// <summary>
        /// Stores the content of a page file revision, replacing anything already stored for it.
        /// The file only appears at its final location once it has been completely written.
        /// </summary>
        /// <param name="fileName">The name of the uploaded file. Only its extension is used.</param>
        Task WriteAsync(int pageFileId, int revision, string fileName, byte[] data);

        /// <summary>
        /// Returns the content of a page file revision, or null if it is not stored here.
        /// </summary>
        Task<byte[]?> ReadAllBytesAsync(int pageFileId, int revision);

        /// <summary>
        /// Deletes the content of a page file revision. Does nothing if it is not stored here.
        /// </summary>
        void Delete(int pageFileId, int revision);

        /// <summary>
        /// Renames the content of a page file revision to another revision of the same file, keeping its extension and
        /// replacing anything already stored as the target revision. Does nothing if the source is not stored here.
        /// </summary>
        void MoveRevision(int pageFileId, int fromRevision, int toRevision);

        /// <summary>
        /// Returns the full path of every stored attachment file (uploads still being written are excluded).
        /// </summary>
        IEnumerable<string> EnumerateFiles();

        /// <summary>
        /// Returns the full path of a file in the staging directory, where uploads are received before they become
        /// a page file revision. The staging directory is on the same volume as the stored files.
        /// </summary>
        /// <param name="fileName">A file name only, never a path.</param>
        string GetIncomingPath(string fileName);

        /// <summary>
        /// Moves a completely received file from the staging directory into place as the content of a page file revision,
        /// replacing anything already stored for that revision. The source file no longer exists afterwards.
        /// </summary>
        /// <param name="fileName">The name of the uploaded file. Only its extension is used.</param>
        void Import(int pageFileId, int revision, string fileName, string incomingPath);

        /// <summary>
        /// Deletes files in the staging directory that have not been written to for the given time: uploads that were abandoned.
        /// </summary>
        /// <returns>The number of files deleted.</returns>
        int DeleteStaleIncoming(TimeSpan olderThan);
    }
}

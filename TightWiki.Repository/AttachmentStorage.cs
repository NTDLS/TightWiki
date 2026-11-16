using System.Globalization;
using System.Text.RegularExpressions;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Repository
{
    /// <summary>
    /// Stores attachment content as one file per page file revision, keeping the extension of the uploaded file:
    ///     {RootPath}/{PageFileId}/{Revision}{.extension}      for example "83/2.mp4"
    /// Only the extension comes from the user, and only if it is plain letters and digits, so names on disk are always safe.
    /// Page file ids are never reused (AUTOINCREMENT), so a path always refers to the same file, including while its page
    ///  sits in the deleted pages database.
    /// The database does not record the extension, so a revision is found by its number whatever its extension is.
    /// </summary>
    public partial class AttachmentStorage
        : ITwAttachmentStorage
    {
        /// <summary>
        /// Files are written here first and then moved into place, so a partially written file is never visible.
        /// It is inside the root so that the move is a rename on the same volume.
        /// </summary>
        private const string IncomingDirectoryName = ".incoming";

        [GeneratedRegex(@"^\.[A-Za-z0-9]{1,16}$")]
        private static partial Regex SafeExtensionPattern();

        public string RootPath { get; private set; }

        private string IncomingPath => Path.Combine(RootPath, IncomingDirectoryName);

        public AttachmentStorage(string rootPath)
        {
            RootPath = Path.GetFullPath(rootPath);
            Directory.CreateDirectory(RootPath);
            Directory.CreateDirectory(IncomingPath);
        }

        /// <summary>
        /// The extension a file named fileName is stored with: its own extension in lower case, or none if it has none
        ///  or it is not plain letters and digits.
        /// </summary>
        public static string GetStoredExtension(string? fileName)
        {
            var extension = Path.GetExtension(fileName ?? string.Empty);
            return SafeExtensionPattern().IsMatch(extension) ? extension.ToLowerInvariant() : string.Empty;
        }

        public string? FindPath(int pageFileId, int revision)
            => FindRevisionFiles(pageFileId, revision).FirstOrDefault();

        public bool Exists(int pageFileId, int revision)
            => FindPath(pageFileId, revision) != null;

        public async Task WriteAsync(int pageFileId, int revision, string fileName, byte[] data)
        {
            var incomingPath = Path.Combine(IncomingPath, $"{Guid.NewGuid():N}.part");

            try
            {
                Directory.CreateDirectory(IncomingPath);
                await File.WriteAllBytesAsync(incomingPath, data);
                MoveIntoPlace(pageFileId, revision, fileName, incomingPath);
            }
            finally
            {
                if (File.Exists(incomingPath))
                {
                    File.Delete(incomingPath);
                }
            }
        }

        public async Task<byte[]?> ReadAllBytesAsync(int pageFileId, int revision)
        {
            var path = FindPath(pageFileId, revision);
            if (path == null)
            {
                return null;
            }

            try
            {
                return await File.ReadAllBytesAsync(path);
            }
            catch (FileNotFoundException)
            {
                return null; //Deleted since it was found.
            }
        }

        public void Delete(int pageFileId, int revision)
        {
            foreach (var path in FindRevisionFiles(pageFileId, revision).ToList())
            {
                File.Delete(path);
            }

            DeleteDirectoryIfEmpty(pageFileId);
        }

        public void MoveRevision(int pageFileId, int fromRevision, int toRevision)
        {
            if (fromRevision == toRevision)
            {
                return;
            }

            var fromPath = FindPath(pageFileId, fromRevision);
            if (fromPath != null)
            {
                MoveIntoPlace(pageFileId, toRevision, fromPath, fromPath);
            }
        }

        public IEnumerable<string> EnumerateFiles()
        {
            if (!Directory.Exists(RootPath))
            {
                yield break;
            }

            foreach (var directory in Directory.EnumerateDirectories(RootPath))
            {
                if (Path.GetFileName(directory).Equals(IncomingDirectoryName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    yield return file;
                }
            }
        }

        public string GetIncomingPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            {
                throw new ArgumentException("Expected a file name without a path.", nameof(fileName));
            }

            Directory.CreateDirectory(IncomingPath);
            return Path.Combine(IncomingPath, fileName);
        }

        public void Import(int pageFileId, int revision, string fileName, string incomingPath)
        {
            var fullIncomingPath = Path.GetFullPath(incomingPath);
            if (!string.Equals(Path.GetDirectoryName(fullIncomingPath), IncomingPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Only files in the staging directory can be imported.", nameof(incomingPath));
            }

            MoveIntoPlace(pageFileId, revision, fileName, fullIncomingPath);
        }

        public int DeleteStaleIncoming(TimeSpan olderThan)
        {
            if (!Directory.Exists(IncomingPath))
            {
                return 0;
            }

            int deleted = 0;
            var threshold = DateTime.UtcNow - olderThan;

            foreach (var file in new DirectoryInfo(IncomingPath).EnumerateFiles())
            {
                try
                {
                    if (file.LastWriteTimeUtc < threshold)
                    {
                        file.Delete();
                        deleted++;
                    }
                }
                catch (IOException)
                {
                    //In use, so not abandoned.
                }
            }

            return deleted;
        }

        /// <summary>
        /// Moves a file into place as a revision, named with the extension of fileName. Anything already stored as that
        ///  revision, under any extension, is replaced, so a revision is only ever one file.
        /// </summary>
        private void MoveIntoPlace(int pageFileId, int revision, string fileName, string sourcePath)
        {
            Directory.CreateDirectory(GetDirectory(pageFileId));

            var path = GetPath(pageFileId, revision, fileName);
            foreach (var existing in FindRevisionFiles(pageFileId, revision).ToList())
            {
                if (!string.Equals(existing, path, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(existing, sourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(existing);
                }
            }

            File.Move(sourcePath, path, overwrite: true);
        }

        private string GetPath(int pageFileId, int revision, string fileName)
            => Path.Combine(GetDirectory(pageFileId), revision.ToString(CultureInfo.InvariantCulture) + GetStoredExtension(fileName));

        /// <summary>
        /// The files stored for a revision: "{revision}" or "{revision}.{extension}". Normally there is at most one.
        /// </summary>
        private IEnumerable<string> FindRevisionFiles(int pageFileId, int revision)
        {
            var directory = GetDirectory(pageFileId);
            if (!Directory.Exists(directory))
            {
                return [];
            }

            var number = revision.ToString(CultureInfo.InvariantCulture);
            return Directory.EnumerateFiles(directory, number + "*").Where(path =>
            {
                var name = Path.GetFileName(path);
                return name == number || (name.StartsWith(number + ".", StringComparison.Ordinal) && name.IndexOf('.', number.Length + 1) < 0);
            });
        }

        private string GetDirectory(int pageFileId)
            => Path.Combine(RootPath, pageFileId.ToString(CultureInfo.InvariantCulture));

        private void DeleteDirectoryIfEmpty(int pageFileId)
        {
            try
            {
                var directory = GetDirectory(pageFileId);
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (IOException)
            {
                //Another file was written to the directory in the meantime, it is still in use.
            }
        }
    }
}

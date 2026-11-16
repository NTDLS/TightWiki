using System.Globalization;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Repository
{
    /// <summary>
    /// Stores attachment content as one file per page file revision:
    ///     {RootPath}/{PageFileId}/{Revision}.bin
    /// No user-supplied names are used on disk. Page file ids are never reused (AUTOINCREMENT), so a path always
    ///  refers to the same file, including while its page sits in the deleted pages database.
    /// </summary>
    public class AttachmentStorage
        : ITwAttachmentStorage
    {
        private const string FileExtension = ".bin";

        /// <summary>
        /// Files are written here first and then moved into place, so a partially written file is never visible.
        /// It is inside the root so that the move is a rename on the same volume.
        /// </summary>
        private const string IncomingDirectoryName = ".incoming";

        public string RootPath { get; private set; }

        private string IncomingPath => Path.Combine(RootPath, IncomingDirectoryName);

        public AttachmentStorage(string rootPath)
        {
            RootPath = Path.GetFullPath(rootPath);
            Directory.CreateDirectory(RootPath);
            Directory.CreateDirectory(IncomingPath);
        }

        public string GetPath(int pageFileId, int revision)
            => Path.Combine(GetDirectory(pageFileId), revision.ToString(CultureInfo.InvariantCulture) + FileExtension);

        public bool Exists(int pageFileId, int revision)
            => File.Exists(GetPath(pageFileId, revision));

        public async Task WriteAsync(int pageFileId, int revision, byte[] data)
        {
            var finalPath = GetPath(pageFileId, revision);
            var incomingPath = Path.Combine(IncomingPath, $"{Guid.NewGuid():N}.part");

            try
            {
                Directory.CreateDirectory(IncomingPath);
                await File.WriteAllBytesAsync(incomingPath, data);

                Directory.CreateDirectory(GetDirectory(pageFileId));
                File.Move(incomingPath, finalPath, overwrite: true);
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
            try
            {
                return await File.ReadAllBytesAsync(GetPath(pageFileId, revision));
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        public void Delete(int pageFileId, int revision)
        {
            var path = GetPath(pageFileId, revision);
            if (File.Exists(path))
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

            var fromPath = GetPath(pageFileId, fromRevision);
            if (File.Exists(fromPath))
            {
                File.Move(fromPath, GetPath(pageFileId, toRevision), overwrite: true);
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

                foreach (var file in Directory.EnumerateFiles(directory, "*" + FileExtension))
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

        public void Import(int pageFileId, int revision, string incomingPath)
        {
            var fullIncomingPath = Path.GetFullPath(incomingPath);
            if (!string.Equals(Path.GetDirectoryName(fullIncomingPath), IncomingPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Only files in the staging directory can be imported.", nameof(incomingPath));
            }

            Directory.CreateDirectory(GetDirectory(pageFileId));
            File.Move(fullIncomingPath, GetPath(pageFileId, revision), overwrite: true);
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

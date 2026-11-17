using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Repository
{
    /// <summary>
    /// What is known about an upload that has been started but not yet completed.
    /// </summary>
    public class TwAttachmentUpload
    {
        public string UploadId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public int PageId { get; set; }
        public string PageNavigation { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Thrown when an upload request can not be accepted. The status code and message are returned to the browser.
    /// </summary>
    public class TwAttachmentUploadException(int statusCode, string message, long? received = null)
        : Exception(message)
    {
        public int StatusCode { get; } = statusCode;

        /// <summary>
        /// How many bytes the server has, so the browser can continue from there.
        /// </summary>
        public long? Received { get; } = received;
    }

    /// <summary>
    /// Receives attachment uploads in pieces, so files of any size can be uploaded without a large request and an upload
    ///  that is interrupted can continue where it stopped.
    /// Each upload is two files in the attachment storage's staging directory: "{UploadId}.upload.json" describes it and
    ///  "{UploadId}.upload.part" receives the content. Keeping this on disk lets an upload continue after the site restarts.
    /// </summary>
    public partial class TwAttachmentUploads(ITwAttachmentStorage storage)
    {
        /// <summary>
        /// The largest piece accepted in one request. It is below IIS's default 30,000,000 byte request limit and
        ///  ASP.NET Core's default 30MB limit, so no server limits need to be raised.
        /// </summary>
        public const int MaxPieceSize = 16 * 1024 * 1024;

        /// <summary>
        /// Uploads that have not received anything for this long are abandoned and deleted.
        /// </summary>
        public static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(24);

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        [GeneratedRegex("^[0-9a-f]{32}$")]
        private static partial Regex UploadIdPattern();

        public ITwAttachmentStorage Storage => storage;

        public TwAttachmentUpload Start(Guid userId, int pageId, string pageNavigation, string fileName, long size)
        {
            storage.DeleteStaleIncoming(AbandonedAfter);

            var upload = new TwAttachmentUpload
            {
                UploadId = Guid.NewGuid().ToString("N"),
                UserId = userId,
                PageId = pageId,
                PageNavigation = pageNavigation,
                FileName = fileName,
                Size = size,
                CreatedDate = DateTime.UtcNow
            };

            File.WriteAllBytes(GetPartPath(upload.UploadId), []);
            File.WriteAllText(GetInfoPath(upload.UploadId), JsonSerializer.Serialize(upload));

            return upload;
        }

        /// <summary>
        /// Returns the upload if it exists and belongs to the user, otherwise throws a 404 so that one user can not
        ///  find out about another user's uploads.
        /// </summary>
        public TwAttachmentUpload Get(string? uploadId, Guid userId)
        {
            if (uploadId == null || !UploadIdPattern().IsMatch(uploadId))
            {
                throw new TwAttachmentUploadException(404, "The upload was not found.");
            }

            try
            {
                var upload = JsonSerializer.Deserialize<TwAttachmentUpload>(File.ReadAllText(GetInfoPath(uploadId)));
                if (upload != null && upload.UserId == userId && File.Exists(GetPartPath(uploadId)))
                {
                    return upload;
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (JsonException)
            {
            }

            throw new TwAttachmentUploadException(404, "The upload was not found.");
        }

        public long GetReceived(string uploadId)
            => new FileInfo(GetPartPath(uploadId)).Length;

        /// <summary>
        /// Adds a piece of the content. The offset must equal the number of bytes already received, so pieces can not
        ///  be skipped or applied twice; when it does not, the response tells the browser where to continue from.
        /// </summary>
        /// <returns>The number of bytes received so far.</returns>
        public async Task<long> AppendAsync(TwAttachmentUpload upload, long offset, Stream content, CancellationToken cancellationToken)
        {
            return await WithLock(upload.UploadId, async () =>
            {
                var partPath = GetPartPath(upload.UploadId);

                await using var stream = new FileStream(partPath, FileMode.Open, FileAccess.Write, FileShare.Read, 81920, FileOptions.Asynchronous);

                if (stream.Length != offset)
                {
                    throw new TwAttachmentUploadException(409,
                        "The piece does not continue where the upload left off.", stream.Length);
                }

                stream.Seek(0, SeekOrigin.End);

                var buffer = new byte[81920];
                long allowed = upload.Size - offset;
                long written = 0;
                int read;

                try
                {
                    while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        written += read;
                        if (written > allowed || written > MaxPieceSize)
                        {
                            throw new TwAttachmentUploadException(400,
                                "The piece is larger than the rest of the file or the largest piece allowed.", offset);
                        }
                        await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    }
                    await stream.FlushAsync(cancellationToken);
                }
                catch
                {
                    //Only whole pieces are kept, so the browser can always resend the piece it was sending.
                    stream.SetLength(offset);
                    throw;
                }

                File.SetLastWriteTimeUtc(GetInfoPath(upload.UploadId), DateTime.UtcNow);
                return stream.Length;
            });
        }

        /// <summary>
        /// Runs the action while no piece is being added to the upload, used to complete or cancel it.
        /// </summary>
        public async Task<T> WithLock<T>(string uploadId, Func<Task<T>> action)
        {
            var uploadLock = _locks.GetOrAdd(uploadId, _ => new SemaphoreSlim(1, 1));
            await uploadLock.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                uploadLock.Release();
            }
        }

        /// <summary>
        /// Removes what is left of an upload: after it was completed (the received file may have been moved into place) or cancelled.
        /// </summary>
        public void Delete(string uploadId)
        {
            foreach (var path in new[] { GetPartPath(uploadId), GetInfoPath(uploadId) })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            _locks.TryRemove(uploadId, out _);
        }

        public string GetPartPath(string uploadId)
            => storage.GetIncomingPath($"{uploadId}.upload.part");

        private string GetInfoPath(string uploadId)
            => storage.GetIncomingPath($"{uploadId}.upload.json");
    }
}

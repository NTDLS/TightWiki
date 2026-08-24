using PagesEntities = TightWiki.Data.EfCore.Entities.Pages;
using DeletedPagesEntities = TightWiki.Data.EfCore.Entities.DeletedPages;
using DeletedPageRevisionsEntities = TightWiki.Data.EfCore.Entities.DeletedPageRevisions;

namespace TightWiki.Data.EfCore.Entities.Users
{
    /// <summary>
    /// The wiki-specific profile data for a user account (Users.Profile), one row per Identity user. <see
    /// cref="UserId"/> matches the corresponding AspNetUsers.Id row, but is modeled as a plain <see cref="Guid"/>
    /// column here, not an EF navigation - AspNetUsers lives in a separate DbContext (ApplicationDbContext), see
    /// Database-Providers-Plan.md chapter 4.1.1.
    /// </summary>
    /// <remarks>
    /// The <c>Pages_*</c>/<c>DeletedPages_*</c>/<c>DeletedPageRevisions_*</c> collections below are the reverse
    /// side of the cross-schema *UserId navigations added to those schemas' entities (see
    /// Database-Providers-Plan.md chapter 4.3) - prefixed by schema because e.g. Pages.Page and
    /// DeletedPages.Page are distinct types that would otherwise both want a property named "Pages"/"CreatedPages".
    /// </remarks>
    public class Profile
    {
        /// <summary>
        /// The unique identifier of the user this profile belongs to (matches AspNetUsers.Id).
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The unique, case-insensitive, URL-safe navigation path used to locate this user's public profile, or
        /// null if not set.
        /// </summary>
        public string? Navigation { get; set; }

        /// <summary>
        /// The unique account name used for login and display, or null if not set.
        /// </summary>
        public string? AccountName { get; set; }

        /// <summary>
        /// A short biography or description provided by the user, or null if not provided.
        /// </summary>
        public string? Biography { get; set; }

        /// <summary>
        /// The user's avatar image bytes, or null if no avatar has been uploaded.
        /// </summary>
        public byte[]? Avatar { get; set; }

        /// <summary>
        /// The date and time this profile was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The date and time this profile was last modified.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// The MIME type of <see cref="Avatar"/>, or null if no avatar has been uploaded.
        /// </summary>
        public string? AvatarContentType { get; set; }

        /// <summary>
        /// The account-level permission records for this user.
        /// </summary>
        public ICollection<AccountPermission> AccountPermissions { get; set; } = [];

        /// <summary>
        /// The role memberships assigned to this user.
        /// </summary>
        public ICollection<AccountRole> AccountRoles { get; set; } = [];

        /// <summary>
        /// The (non-deleted) pages originally created by this user (Pages.Page.CreatedByUserId).
        /// </summary>
        public ICollection<PagesEntities.Page> Pages_CreatedPages { get; set; } = [];

        /// <summary>
        /// The (non-deleted) pages last modified by this user (Pages.Page.ModifiedByUserId).
        /// </summary>
        public ICollection<PagesEntities.Page> Pages_ModifiedPages { get; set; } = [];

        /// <summary>
        /// The comments posted by this user on (non-deleted) pages (Pages.PageComment.UserId).
        /// </summary>
        public ICollection<PagesEntities.PageComment> Pages_PageComments { get; set; } = [];

        /// <summary>
        /// The page revisions made by this user on (non-deleted) pages (Pages.PageRevision.ModifiedByUserId).
        /// </summary>
        public ICollection<PagesEntities.PageRevision> Pages_PageRevisions { get; set; } = [];

        /// <summary>
        /// The file revisions uploaded by this user on (non-deleted) pages
        /// (Pages.PageFileRevision.CreatedByUserId).
        /// </summary>
        public ICollection<PagesEntities.PageFileRevision> Pages_PageFileRevisions { get; set; } = [];

        /// <summary>
        /// The "currently editing" session records for this user (Pages.CurrentPageEditors.UserId).
        /// </summary>
        public ICollection<PagesEntities.CurrentPageEditor> Pages_CurrentPageEditors { get; set; } = [];

        /// <summary>
        /// The soft-deleted pages originally created by this user (DeletedPages.Page.CreatedByUserId).
        /// </summary>
        public ICollection<DeletedPagesEntities.Page> DeletedPages_CreatedPages { get; set; } = [];

        /// <summary>
        /// The soft-deleted pages last modified by this user before deletion
        /// (DeletedPages.Page.ModifiedByUserId).
        /// </summary>
        public ICollection<DeletedPagesEntities.Page> DeletedPages_ModifiedPages { get; set; } = [];

        /// <summary>
        /// The comments posted by this user that were carried over onto soft-deleted pages
        /// (DeletedPages.PageComment.UserId).
        /// </summary>
        public ICollection<DeletedPagesEntities.PageComment> DeletedPages_PageComments { get; set; } = [];

        /// <summary>
        /// The page revisions made by this user that were carried over onto soft-deleted pages
        /// (DeletedPages.PageRevision.ModifiedByUserId).
        /// </summary>
        public ICollection<DeletedPagesEntities.PageRevision> DeletedPages_PageRevisions { get; set; } = [];

        /// <summary>
        /// The file revisions uploaded by this user that were carried over onto soft-deleted pages
        /// (DeletedPages.PageFileRevision.CreatedByUserId).
        /// </summary>
        public ICollection<DeletedPagesEntities.PageFileRevision> DeletedPages_PageFileRevisions { get; set; } = [];

        /// <summary>
        /// The page deletions performed by this user (DeletedPages.DeletionMeta.DeletedByUserId).
        /// </summary>
        public ICollection<DeletedPagesEntities.DeletionMeta> DeletedPages_DeletionMetas { get; set; } = [];

        /// <summary>
        /// The individually-deleted page revisions made by this user
        /// (DeletedPageRevisions.PageRevision.ModifiedByUserId).
        /// </summary>
        public ICollection<DeletedPageRevisionsEntities.PageRevision> DeletedPageRevisions_PageRevisions { get; set; } = [];

        /// <summary>
        /// The individual page revision deletions performed by this user
        /// (DeletedPageRevisions.DeletionMeta.DeletedByUserId).
        /// </summary>
        public ICollection<DeletedPageRevisionsEntities.DeletionMeta> DeletedPageRevisions_DeletionMetas { get; set; } = [];
    }
}

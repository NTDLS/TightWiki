using System.Collections.Generic;
using System.Linq;
using TightWiki.Library.DataModels;
using TightWiki.Library.DataStorage;

namespace TightWiki.Library.Repository
{
    public static class PageTagRepository
    {
        public static List<TagAssociation> GetAssociatedTags(string groupName)
        {
            var param = new
            {
                GroupName = groupName
            };

            return ManagedDataStorage.Default.Query<TagAssociation>("GetAssociatedTags", param).ToList();
        }

        public static List<Page> GetPageInfoByNamespaces(List<string> namespaces)
        {
            return ManagedDataStorage.Default.Ephemeral(o =>
            {
                using var tempTable = o.CreateValueListTableFrom("TempNamespaces", namespaces);
                return o.Query<Page>("GetPageInfoByNamespaces").ToList();
            });
        }

        public static List<Page> GetPageInfoByTags(List<string> tags)
        {
            return ManagedDataStorage.Default.Ephemeral(o =>
            {
                using var tempTable = o.CreateValueListTableFrom("TempTags", tags);
                return o.Query<Page>("GetPageInfoByTags").ToList();
            });
        }

        public static List<Page> GetPageInfoByTag(string tag)
        {
            return ManagedDataStorage.Default.Ephemeral(o =>
            {
                using var tempTable = o.CreateValueListTableFrom("TempTags", new List<string> { tag });
                return o.Query<Page>("GetPageInfoByTags").ToList();
            });
        }

        public static void UpdatePageTags(int pageId, List<string> tags)
        {
            ManagedDataStorage.Default.Ephemeral(o =>
            {
                using var tempTable = o.CreateValueListTableFrom("TempTags", tags);

                var param = new
                {
                    PageId = pageId
                };

                return o.Query<Page>("UpdatePageTags", param).ToList();
            });
        }
    }
}

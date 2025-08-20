using NTDLS.SqliteDapperWrapper;

namespace TightWiki.Repository
{
    internal static class RepositoryHelper
    {
        /// <summary>
        /// Fills in a custom orderby on a given sql script.
        /// </summary>
        public static string TransposeOrderby(string filename, string? orderBy, string? orderByDirection)
        {
            var script = SqliteManagedInstance.TranslateSqlScript(filename);

            if (string.IsNullOrEmpty(orderBy))
            {
                return script;
            }

            string beginParentTag = "--CUSTOM_ORDER_BEGIN::";
            string endParentTag = "--::CUSTOM_ORDER_BEGIN";

            string beginConfigTag = "--CONFIG::";
            string endConfigTag = "--::CONFIG";

            while (true)
            {
                int beginParentIndex = script.IndexOf(beginParentTag, StringComparison.InvariantCultureIgnoreCase);
                int endParentIndex = script.IndexOf(endParentTag, StringComparison.InvariantCultureIgnoreCase);

                if (beginParentIndex > 0 && endParentIndex > beginParentIndex)
                {
                    var sectionText = script.Substring(beginParentIndex + beginParentTag.Length, (endParentIndex - beginParentIndex) - endParentTag.Length).Trim();

                    int beginConfigIndex = sectionText.IndexOf(beginConfigTag, StringComparison.InvariantCultureIgnoreCase);
                    int endConfigIndex = sectionText.IndexOf(endConfigTag, StringComparison.InvariantCultureIgnoreCase);

                    if (beginConfigIndex >= 0 && endConfigIndex > beginConfigIndex)
                    {
                        var configText = sectionText.Substring(beginConfigIndex + beginConfigTag.Length, (endConfigIndex - beginConfigIndex) - endConfigTag.Length).Trim();

                        var configs = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

                        configs.Remove("/^");
                        configs.Remove("*/");

                        foreach (var line in configText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (line == "/*" || line == "*/" || line.StartsWith("--"))
                            {
                                continue;
                            }

                            int idx = line.IndexOf('=');
                            if (idx > -1)
                            {
                                var key = line.Substring(0, idx).Trim();
                                var value = line.Substring(idx + 1).Trim();
                                configs[key] = value;
                            }
                            else
                            {
                                throw new Exception($"Invalid configuration line in '{filename}': {line}");
                            }
                        }

                        if(!configs.TryGetValue(orderBy, out string? field))
                        {
                            throw new Exception($"No order by mapping was found in '{filename}' for the field '{orderBy}'.");
                        }

                        script = script.Substring(0, beginParentIndex)
                            + $"ORDER BY\r\n\t{field} "
                            + (string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase) ? "asc" : "desc")
                            + script.Substring(endParentIndex + endParentTag.Length);
                    }
                    else
                    {
                        throw new Exception($"No order configuration was found in '{filename}'.");
                    }
                }
                else
                {
                    break;
                }
            }

            return script;
        }
    }
}

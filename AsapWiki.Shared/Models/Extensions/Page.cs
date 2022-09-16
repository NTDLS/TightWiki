using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AsapWiki.Shared.Models
{
    public partial class Page : BaseModel
    {
        public List<string> HashTags()
        {
            Regex rgx = new Regex(@"(\#\#tags.+?\(\))|(\#\#tags.*?\(.*?\))", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(Body);
            foreach (Match match in matches)
            {
                string args = match.Value.Trim();

                int startIndex = args.IndexOf('(');
                int endIndex = args.LastIndexOf(')');

                if (startIndex >= 0 && endIndex > 0 && endIndex > startIndex)
                {
                    args = args.Substring(startIndex + 1, (endIndex - startIndex) - 1).Trim();
                    var tags = args.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    var listOf = new List<string>();
                    foreach (var tag in tags)
                    {
                        listOf.Add(tag.Trim());
                    }
                    return listOf.Distinct().ToList();
                }
            }

            return new List<string>();
        }
    }
}
using AsapWiki.Shared.Wiki;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace AsapWiki.Shared.Models
{
    public partial class Page : BaseModel
    {
        public List<string> HashTags()
        {
            var tags = new List<string>();
            Regex rgx = new Regex(@"(?:\s|^)#[A-Za-z0-9\-_\.]+", RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(Body);
            foreach (Match match in matches)
            {
                string tag = match.Value.Trim(new char[] { '#', '\r', '\n', ' ' });

                //If the hash tag contains a _ or a - then we will treat it as an explcit tag and not try to parse it.
                if (tag.Contains("_") || tag.Contains("-"))
                {
                    tags.Add(tag.Replace('_', ' ').Replace('-', ' ').Replace("  ", " ").Trim());
                }
                else
                {
                    var parsedTag = Utility.SplitCamelCase(tag);
                    var casedTag = Utility.TitleCase(parsedTag);
                    tags.Add(casedTag);
                }
            }

            return tags;
        }
    }
}
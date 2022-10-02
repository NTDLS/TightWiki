using AsapWiki.Shared.Library;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Repository;
using AsapWiki.Shared.Wiki;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WikipediaParser
{
    /// <summary>
    /// This program is used to populate the ASAPWiki database with some data so I can test stuff.
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            //This is just the files that I downoaded from wikipedia using HTTPTracks.
            var files = new List<string>(Directory.EnumerateFiles(@"C:\My Web Sites\Wiki Project\en.wikipedia.org\wiki"));

            foreach (var file in files)
            {
                string pageName = String.Join(" ", Path.GetFileNameWithoutExtension(file).Split('_'));
                string pageNavigation = WikiUtility.CleanPartialURI(pageName);
                string description = string.Empty;
                var parsedContent = new StringBuilder();
                var allContent = File.ReadAllText(file);

                int titleIndex = allContent.IndexOf("<title>");
                if (titleIndex > -1)
                {
                    int titleEndIndex = allContent.IndexOf("</title>");
                    if (titleEndIndex > -1)
                    {
                        description = allContent.Substring(titleIndex + 7, (titleEndIndex - titleIndex) - 7);
                    }
                }

                int bodyIndex = allContent.IndexOf("<body");
                if (bodyIndex > -1)
                {
                    bodyIndex = allContent.IndexOf('>', bodyIndex) + 1;

                    int bodyEndIndex = allContent.IndexOf("</body>");
                    if (bodyEndIndex > -1)
                    {
                        parsedContent = new StringBuilder(allContent.Substring(bodyIndex, (bodyEndIndex - bodyIndex)));
                    }
                }

                if (parsedContent.Length == 0)
                {
                    continue;
                }

                parsedContent.Insert(0, "##title()\r\n##{{{(Panel, Table of Contents) ##toc() }}}\r\n");

                var preProcessPageTokens = AsapWiki.Shared.Wiki.WikiUtility.ParsePageTokens(parsedContent.ToString());

                if (preProcessPageTokens.Any(o => o.Token == "draft"))
                {
                    parsedContent.Insert(0, "@@draft\r\n");
                }
                if (preProcessPageTokens.Any(o => o.Token == "deprecate"))
                {
                    parsedContent.Insert(0, "@@deprecate\r\n");
                }

                var mockTags = preProcessPageTokens.OrderByDescending(o => o.Weight).ToList().Where(o => o.Token.Length > 4 && o.Weight > 50).Take(10).ToList();
                if (mockTags.Count > 0)
                {
                    parsedContent.Insert(0, "##SetTags(" + String.Join(" | ", mockTags.Select(o=>o.Token)) + ")\r\n");
                }

                int indexOfH = parsedContent.ToString().IndexOf("<h2");
                if (indexOfH > 0)
                {
                    int indexEndOfH = parsedContent.ToString().IndexOf("</h2>", indexOfH);
                    string heading = parsedContent.ToString().Substring(indexOfH, (indexEndOfH - indexOfH) + 5);
                    string newHeading = HTML.StripHtml(heading);
                    parsedContent.Replace(heading, $"=={newHeading}");
                }

                parsedContent.AppendLine("\r\n==Related\r\n##related-full()");

                var page = new Page()
                {
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = 1,
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedByUserId = 1,
                    Body = parsedContent.ToString(),
                    Name = pageName,
                    Navigation = pageNavigation,
                    Description = description
                };

                int? pageId = PageRepository.GetPageByNavigation(pageNavigation)?.Id;
                if (pageId != null)
                {
                    page.Id = (int)pageId;
                }

                Console.WriteLine($"Saving [{page.Name}]");

                page.Id = PageRepository.SavePage(page);

                var wikifier = new Wikifier(page);
                PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
                PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);
                var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
                PageRepository.SavePageTokens(pageTokens);
            }
        }
    }
}

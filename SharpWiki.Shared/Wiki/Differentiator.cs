using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpWiki.Shared.Wiki
{
    public static class Differentiator
    {
        /// <summary>
        /// This leaves alot to be desired.
        /// </summary>
        /// <param name="thisRev"></param>
        /// <param name="prevRev"></param>
        /// <returns></returns>
        public static string GetComparisionSummary(string thisRev, string prevRev)
        {
            var summary = new StringBuilder();

            var thisRevLines = thisRev.Split('\n');
            var prevRevLines = prevRev.Split('\n');
            int thisRevLineCount = thisRevLines.Count();
            int prevRevLinesCount = prevRevLines.Count();

            int maxLines = (thisRevLineCount > prevRevLinesCount) ? thisRevLineCount : prevRevLinesCount;

            int differences = 0;

            for (int i = 0; i < maxLines; i++)
            {
                if (thisRevLineCount > i && prevRevLinesCount > i)
                {
                    if (thisRevLines[i] != prevRevLines[i])
                    {
                        differences++;
                    }
                }
            }

            if (differences > 0)
            {
                summary.Append($"{differences:N0} lines changed.");
            }

            if (thisRevLineCount > prevRevLinesCount)
            {
                if (summary.Length > 0) summary.Append(" ");
                summary.Append($"{(thisRevLineCount - prevRevLinesCount):N0} lines added.");
            }
            else if (prevRevLinesCount > thisRevLineCount)
            {
                if (summary.Length > 0) summary.Append(" ");
                summary.Append($"{(prevRevLinesCount - thisRevLineCount):N0} lines deleted.");
            }

            if (summary.Length == 0)
            {
                summary.Append($"No changes detected.");
            }

            return summary.ToString();
        }
    }
}

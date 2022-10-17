using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TightWiki.Shared.Wiki
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

            int linesAdded = prevRevLines.Except(thisRevLines).Count();
            int linesDeleted = thisRevLines.Except(prevRevLines).Count();

            if(thisRevLineCount != prevRevLinesCount)
            {
                summary.Append($"{Math.Abs(thisRevLineCount - prevRevLinesCount):N0} lines changed.");
            }

            if (linesAdded > 0)
            {
                if (summary.Length > 0) summary.Append(" ");
                summary.Append($"{linesAdded:N0} lines added.");
            }

            if (linesDeleted > 0)
            {
                if (summary.Length > 0) summary.Append(" ");
                summary.Append($"{linesDeleted:N0} lines deleted.");
            }

            if (summary.Length == 0)
            {
                summary.Append($"No changes detected.");
            }

            return summary.ToString();
        }
    }
}

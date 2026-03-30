namespace TightWiki.Plugin.Models
{
    public class TwTagCloudItem
    {
        public string Name = "";
        public string HTML = "";
        public int Rank = 0;

        public TwTagCloudItem(string name, int rank, string html)
        {
            Name = name;
            HTML = html;
            Rank = rank;
        }

        public static int CompareItem(TwTagCloudItem x, TwTagCloudItem y)
        {
            return string.Compare(x.Name, y.Name);
        }
    }
}

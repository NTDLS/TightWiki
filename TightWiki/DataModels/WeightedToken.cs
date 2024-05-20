using TightWiki.DataModels;

namespace TightWiki.Models
{
    public class WeightedToken
    {
        public string Token { get; set; } = string.Empty;
        public double Weight { get; set; }
        public string DoubleMetaphone { get; set; } = string.Empty;

        public PageToken ToPageToken(int pageId)
        {
            return new PageToken
            {
                PageId = pageId,
                Token = Token,
                DoubleMetaphone = DoubleMetaphone,
                Weight = Weight
            };
        }
    }
}

using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models
{
    public class WeightedToken
    {
        public string Token { get; set; }
        public int Weight { get; set; }
        public string DoubleMetaphone { get; set; }

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

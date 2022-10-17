using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models
{
    public class WeightedToken
    {
        public string Token;
        public int Weight;

        public PageToken ToPageToken(int pageId)
        {
            return new PageToken
            {
                PageId = pageId,
                Token = Token,
                Weight = Weight
            };
        }
    }
}

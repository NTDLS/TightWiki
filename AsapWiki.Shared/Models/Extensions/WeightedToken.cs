using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Models
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

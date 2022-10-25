using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TightWiki.Shared.Models.View
{
    public class ConfirmActionModel : ModelBase
    {
        public string ActionToConfirm { get; set; }
        public string PostBackURL { get; set; }
        public string Message { get; set; }
    }
}

using TightWiki.Engine.Library.Interfaces;
using TightWiki.Repository;

namespace TightWiki.Engine.Handlers
{
    public class ExceptionHandler : IExceptionHandler
    {
        public void Log(IWikifier wikifier, Exception? ex, string customText)
        {
            if (ex != null)
            {
                ExceptionRepository.InsertException(ex, customText);
            }

            ExceptionRepository.InsertException(customText);
        }
    }
}

using TightWiki.Engine.Library.Interfaces;
using TightWiki.Repository;

namespace TightWiki.Engine.Implementation
{
    public class ExceptionHandler : IExceptionHandler
    {
        public void Log(ITightEngineState state, Exception? ex, string customText)
        {
            if (ex != null)
            {
                ExceptionRepository.InsertException(ex, customText);
            }

            ExceptionRepository.InsertException(customText);
        }
    }
}

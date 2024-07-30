namespace TightWiki.Engine.Library.Interfaces
{
    public interface IExceptionHandler
    {
        public void Log(ITightEngineState state, Exception? ex, string customText);
    }
}

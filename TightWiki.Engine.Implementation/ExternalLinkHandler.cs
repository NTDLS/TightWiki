﻿using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Implementation
{
    public class ExternalLinkHandler : IExternalLinkHandler
    {
        public HandlerResult Handle(ITightEngineState state, string link, string? text, string? image)
        {
            if (string.IsNullOrEmpty(image))
            {
                return new HandlerResult($"<a href=\"{link}\">{text}</a>")
                {
                    Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                };
            }
            else
            {
                return new HandlerResult($"<a href=\"{link}\"><img src=\"{image}\" border =\"0\"></a>")
                {
                    Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                };
            }
        }
    }
}
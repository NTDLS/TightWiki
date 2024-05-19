using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;

namespace TightWiki.Library.ViewModels
{
    public class ViewModelBase
    {
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public bool ValidateModelAndSetErrors(ModelStateDictionary modelState)
        {
            if (modelState.IsValid == false)
            {
                var allErrors = new List<string>();

                foreach (var state in modelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        allErrors.Add($"<strong>{state.Key}</strong>: {error.ErrorMessage}");
                    }
                }

                ErrorMessage = string.Join("<br />\r\n", allErrors);
                return false;
            }

            return true;
        }
    }
}

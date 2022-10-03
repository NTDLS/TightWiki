using System;
using System.IO;
using System.Linq;
using System.Web;

namespace AsapWiki.Shared.Library
{
    public static class Utility
    {
        public static byte[] ConvertHttpFileToBytes(HttpPostedFileBase image)
        {
            using (BinaryReader reader = new BinaryReader(image.InputStream))
            {
                byte[] imageBytes = reader.ReadBytes((int)image.ContentLength);
                return imageBytes;
            }
        }

        public static T ConvertTo<T>(string value)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else if (typeof(T) == typeof(int))
            {
                if (int.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Error converting value [{value}] to integer.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else if (typeof(T) == typeof(float))
            {
                if (float.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Error converting value [{value}] to float.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else if (typeof(T) == typeof(double))
            {
                if (double.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Error converting value [{value}] to double.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else if (typeof(T) == typeof(bool))
            {
                value = value.ToLower();

                if (value.All(char.IsNumber))
                {
                    if (int.Parse(value) != 0)
                        value = "true";
                    else
                        value = "false";
                }

                if (bool.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Error converting value [{value}] to boolean.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else
            {
                throw new Exception($"Unsupported conversion type.");
            }
        }
    }
}

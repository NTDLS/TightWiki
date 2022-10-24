using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace TightWiki.Shared.Library
{
    public static class ExtensionMethods
    {
        public static bool IsNull<T>(this T @this) where T : class
        {
            return @this == null;
        }

        public static T IsNull<T>(this T @this, T defaultValue) where T : class
        {
            return @this == null ? defaultValue : @this;
        }

        public static bool IsNullOrEmpty(this string @this)
        {
            return string.IsNullOrEmpty(@this);
        }

        public static string IsNullOrEmpty(this string @this, string defaultValue)
        {
            return string.IsNullOrEmpty(@this) == true ? defaultValue : @this;
        }

        public static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        public static DataTable ToDataTable<T>(this List<T> iList)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();

            foreach (PropertyDescriptor prop in properties)
            {
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (T item in iList)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
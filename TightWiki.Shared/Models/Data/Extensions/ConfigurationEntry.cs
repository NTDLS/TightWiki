using System.Web.Mvc;
using TightWiki.Shared.Library;

namespace TightWiki.Shared.Models.Data
{
    public partial class ConfigurationEntry
	{
		public int Id { get; set; }
		public int ConfigurationGroupId { get; set; }
		public string Name { get; set; }

		[AllowHtml]
		public string Value { get; set; }
		public int DataTypeId { get; set; }
		public string Description { get; set; }
		public bool IsEncrypted { get; set; }

        public T As<T>()
        {
            return Utility.ConvertTo<T>(Value);
        }

        public T As<T>(T defaultValue)
        {
            if (Value == null)
            {
                return defaultValue;
            }

            return Utility.ConvertTo<T>(Value);
        }

        public string DataType { get; set; }
    }
}
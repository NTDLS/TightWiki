using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class DatabaseBackupsViewModel
        : TwViewModel
    {
        public string BackupPath { get; set; } = string.Empty;
        public List<TwDatabaseBackup> Backups { get; set; } = new();
    }
}

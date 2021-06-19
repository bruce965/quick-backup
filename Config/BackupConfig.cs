using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuickBackup.Config
{
	public class BackupConfig
	{
		[JsonPropertyName("backups")]
		public IList<Backup> Backups { get; set; }
			= new List<Backup>();
	}
}

using System;
using System.IO;

namespace QuickBackup.Data
{
	public record BackupInfo(
		DirectoryInfo Path,
		DateTime Date,
		BackupType Type);
}

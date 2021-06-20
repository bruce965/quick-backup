using System;
using System.IO;

namespace QuickBackup.Data
{
	public record BackupInfo(
		DirectoryInfo Path,
		DateTime Date,
		BackupType Type)
	{
		public bool IsPartial { get; init; }

		public bool IsOnDemand => Type == BackupType.OnDemand;

		public bool IsAtBoot => Type.HasFlag(BackupType.AtBoot);

		public bool IsYearly => Type.HasFlag(BackupType.Yearly);

		public bool IsMonthly => Type.HasFlag(BackupType.Monthly);

		public bool IsWeekly => Type.HasFlag(BackupType.Weekly);

		public bool IsDaily => Type.HasFlag(BackupType.Daily);

		public bool IsHourly => Type.HasFlag(BackupType.Hourly);
	};
}

using System;

namespace QuickBackup.Data
{
	[Flags]
	public enum BackupType
	{
		/// <summary>
		/// On-demand backup, unscheduled.
		/// </summary>
		OnDemand = 0,

		/// <summary>
		/// First backup after boot.
		/// </summary>
		AtBoot = 1 << 0,

		Yearly = 1 << 1,

		Monthly = 1 << 2,

		Weekly = 1 << 3,

		Daily = 1 << 4,

		Hourly = 1 << 5,
	}
}

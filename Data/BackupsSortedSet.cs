using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuickBackup.Utility;

namespace QuickBackup.Data
{
	/// <summary>
	/// Set of backups, sorted from oldest to most recent.
	/// </summary>
	public class BackupsSortedSet : SortedSet<BackupInfo>
	{
		class ItemComparer : IComparer<BackupInfo>
		{
			public static ItemComparer Instance { get; }
				= new ItemComparer();

			protected ItemComparer() { }

			public int Compare(BackupInfo? x, BackupInfo? y)
			{
				if (x == null)
				{
					if (y == null)
						return 0;

					return -1;
				}
				else if (y == null)
				{
					return +1;
				}
				
				return x.Date.CompareTo(y.Date);
			}
		}

		public BackupsSortedSet()
			: base(ItemComparer.Instance) { }

		public BackupInfo? Find(DateTime date, BackupType type, DayOfWeek firstDayOfWeek)
		{
			switch (type)
			{
				case BackupType.OnDemand:
					return this.FirstOrDefault(b => b.Type == BackupType.OnDemand && b.Date == date);

				case BackupType.AtBoot:
					var bootDate = DateTime.Now - SystemUptime.GetUptime();  // assuming the clock didn't change since boot
					if (date < bootDate)
						return this.LastOrDefault(b => b.Type.HasFlag(BackupType.AtBoot) && b.Date <= date);
					else
						return this.FirstOrDefault(b => b.Type.HasFlag(BackupType.AtBoot) && b.Date >= bootDate);

				case BackupType.Yearly:
					return this.FirstOrDefault(b => b.Type.HasFlag(BackupType.Yearly) && b.Date.Year == date.Year);

				case BackupType.Monthly:
					return this.FirstOrDefault(b => b.Type.HasFlag(BackupType.Monthly) && b.Date.Year == date.Year && b.Date.Month == date.Month);

				case BackupType.Weekly:
					var calendar = CultureInfo.InvariantCulture.Calendar;
					return this.FirstOrDefault(b => b.Type.HasFlag(BackupType.Weekly) && b.Date.Year == date.Year && calendar.GetWeekOfYear(b.Date, CalendarWeekRule.FirstDay, firstDayOfWeek) == calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, firstDayOfWeek));

				case BackupType.Daily:
					return this.FirstOrDefault(b => b.Type.HasFlag(BackupType.Daily) && b.Date.Date == date.Date);

				case BackupType.Hourly:
					return this.FirstOrDefault(b => b.Type.HasFlag(BackupType.Hourly) && b.Date.Date == date.Date && b.Date.Hour == date.Hour);

				default:
					throw new ArgumentException($"Cannot search for a backup of type '{type}'.", nameof(type));
			};
		}
	}
}

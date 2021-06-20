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
		class SortByDate : IComparer<BackupInfo>
		{
			public static SortByDate Instance { get; }
				= new SortByDate();

			protected SortByDate() { }

			int IComparer<BackupInfo>.Compare(BackupInfo? x, BackupInfo? y)
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

		/// <summary>
		/// Get latest backup of any type, preferably excluding partial backups.
		/// </summary>
		public BackupInfo? Latest
			=> this.LastOrDefault(b => !b.IsPartial) ?? this.LastOrDefault();

		public BackupsSortedSet()
			: base(SortByDate.Instance) { }

		/// <summary>
		/// Find a backup by date and type, preferably excluding partial backups.
		/// </summary>
		/// <param name="date">Backup date.</param>
		/// <param name="type">Backup type.</param>
		/// <param name="firstDayOfWeek">First day of the week (used for weekly backups).</param>
		/// <param name="allowPartial">Accept partial backups (only if no complete backup is available).</param>
		/// <returns>Backup by date and type, preferably not partial.</returns>
		public BackupInfo? Find(DateTime date, BackupType type, DayOfWeek firstDayOfWeek = default, bool allowPartial = false)
			=> Find(this.Where(b => !b.IsPartial), date, type, firstDayOfWeek) ?? (allowPartial ? Find(this, date, type, firstDayOfWeek) : null);

		static BackupInfo? Find(IEnumerable<BackupInfo> backups, DateTime date, BackupType type, DayOfWeek firstDayOfWeek)
		{
			switch (type)
			{
				case BackupType.OnDemand:
					return backups.FirstOrDefault(b => b.IsOnDemand && b.Date == date);

				case BackupType.AtBoot:
					var bootDate = DateTime.Now - SystemUptime.GetUptime();  // assuming the clock didn't change since boot
					if (date < bootDate)
						return backups.LastOrDefault(b => b.IsAtBoot && b.Date <= date);
					else
						return backups.FirstOrDefault(b => b.IsAtBoot && b.Date >= bootDate);

				case BackupType.Yearly:
					return backups.FirstOrDefault(b => b.IsYearly && b.Date.Year == date.Year);

				case BackupType.Monthly:
					return backups.FirstOrDefault(b => b.IsMonthly && b.Date.Year == date.Year && b.Date.Month == date.Month);

				case BackupType.Weekly:
					var calendar = CultureInfo.InvariantCulture.Calendar;
					return backups.FirstOrDefault(b => b.IsWeekly && b.Date.Year == date.Year && calendar.GetWeekOfYear(b.Date, CalendarWeekRule.FirstDay, firstDayOfWeek) == calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, firstDayOfWeek));

				case BackupType.Daily:
					return backups.FirstOrDefault(b => b.IsDaily && b.Date.Date == date.Date);

				case BackupType.Hourly:
					return backups.FirstOrDefault(b => b.IsHourly && b.Date.Date == date.Date && b.Date.Hour == date.Hour);

				default:
					throw new ArgumentException($"Cannot search for a backup of type '{type}'.", nameof(type));
			};
		}
	}
}

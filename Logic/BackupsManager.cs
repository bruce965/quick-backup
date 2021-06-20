using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuickBackup.Config;
using QuickBackup.Data;

namespace QuickBackup.Logic
{
	public class BackupsManager
	{
		public string BasePath { get; }

		public BackupConfig Config { get; }

		public Backup Backup { get; }

		public bool DryRun { get; init; }

		Task<BackupsSortedSet>? backups;

		public BackupsManager(string basePath, BackupConfig config, Backup backup)
		{
			BasePath = basePath;
			Config = config;
			Backup = backup;
		}

		public Task<BackupsSortedSet> GetBackupsSetAsync()
		{
			if (backups == null)
				backups = EnumerateBackupsAsync();

			return backups;
		}

		public async Task CleanOldBackupsAsync()
		{
			var targetPath = Path.Combine(BasePath, Backup.TargetPath.ToString());

			await Console.Out.WriteLineAsync($"Cleaning old backups from '{targetPath}'...{(DryRun ? " (DRY-RUN)" : "")}");

			var backups = await GetBackupsSetAsync();

			var remainingOnDemand = Backup.OnDemandCount;
			var remainingAtBoot = Backup.AtBootCount;
			var remainingYearly = Backup.YearlyCount;
			var remainingMonthly = Backup.MonthlyCount;
			var remainingWeekly = Backup.WeeklyCount;
			var remainingDaily = Backup.DailyCount;
			var remainingHourly = Backup.HourlyCount;

			var remainingOnDemandPlusPartial = remainingOnDemand;
			var remainingAtBootPlusPartial = remainingAtBoot;
			var remainingYearlyPlusPartial = remainingYearly;
			var remainingMonthlyPlusPartial = remainingMonthly;
			var remainingWeeklyPlusPartial = remainingWeekly;
			var remainingDailyPlusPartial = remainingDaily;
			var remainingHourlyPlusPartial = remainingHourly;

			var backupsSortedByMostRecent = backups.Reverse().ToList();
			foreach (var backup in backupsSortedByMostRecent)
			{
				var keep = false;

				if ((remainingOnDemand > 0 || remainingOnDemand == null) && backup.IsOnDemand)
				{
					if (backup.IsPartial)
					{
						if (remainingOnDemandPlusPartial > 0)
						{
							remainingOnDemandPlusPartial--;
							keep = true;
						}
					}
					else
					{
						remainingOnDemand--;
						remainingOnDemandPlusPartial--;
						keep = true;
					}
				}

				if (remainingAtBoot > 0 && backup.IsAtBoot)
				{
					if (backup.IsPartial)
					{
						if (remainingAtBootPlusPartial > 0)
						{
							remainingAtBootPlusPartial--;
							keep = true;
						}
					}
					else
					{
						remainingAtBoot--;
						remainingAtBootPlusPartial--;
						keep = true;
					}
				}

				if (remainingYearly > 0 && backup.IsYearly)
				{
					if (backup.IsPartial)
					{
						if (remainingYearlyPlusPartial > 0)
						{
							remainingYearlyPlusPartial--;
							keep = true;
						}
					}
					else
					{
						remainingYearly--;
						remainingYearlyPlusPartial--;
						keep = true;
					}
				}

				if (remainingMonthly > 0 && backup.IsMonthly)
				{
					if (backup.IsPartial)
					{
						if (remainingMonthlyPlusPartial > 0)
						{
							remainingMonthlyPlusPartial--;
							keep = true;
						}
					}
					else
					{
						remainingMonthly--;
						remainingMonthlyPlusPartial--;
						keep = true;
					}
				}

				if (remainingWeekly > 0 && backup.IsWeekly)
				{
					if (backup.IsPartial)
					{
						if (remainingWeeklyPlusPartial > 0)
						{
							remainingWeeklyPlusPartial--;
							keep = true;
						}
					}
					else
					{
						remainingWeekly--;
						remainingWeeklyPlusPartial--;
						keep = true;
					}
				}

				if (remainingDaily > 0 && backup.IsDaily)
				{
					if (backup.IsPartial)
					{
						if (remainingDailyPlusPartial > 0)
						{
							remainingDailyPlusPartial--;
							keep = true;
						}
					}
					else
					{
						remainingDaily--;
						remainingDailyPlusPartial--;
						keep = true;
					}
				}

				if (remainingHourly > 0 && backup.IsHourly)
				{
					if (backup.IsPartial)
					{
						if (remainingHourlyPlusPartial > 0)
						{
							remainingHourlyPlusPartial--;
							keep = true;
						}
					}
					else
					{
						remainingHourly--;
						remainingHourlyPlusPartial--;
						keep = true;
					}
				}

				if (!keep)
				{
					await Console.Out.WriteAsync($"Cleaning '{backup.Path}'...");

					if (!DryRun)
					{
						var partialPath = backup.Path.ToString() + Common.PartialSuffix;
						backup.Path.MoveTo(partialPath);  // async currently not supported
						Directory.Delete(partialPath, true);  // async currently not supported
					}

					await Console.Out.WriteLineAsync(" ok");

				}
			}

			await Console.Out.WriteLineAsync("Done.");
		}

		/// <summary>
		/// Find all saved backups and build a sorted set.
		/// </summary>
		Task<BackupsSortedSet> EnumerateBackupsAsync()
		{
			var targetPath = Path.Combine(BasePath, Backup.TargetPath.ToString());

			var directories = new DirectoryInfo(targetPath)
				.EnumerateDirectories()  // async currently not supported
				.OrderBy(d => d.Name, StringComparer.InvariantCulture);  // name is a sortable date

			var backups = new BackupsSortedSet();

			var calendar = CultureInfo.InvariantCulture.Calendar;

			foreach (var directory in directories)
			{
				var name = directory.Name;

				var isPartial = name.EndsWith(Common.PartialSuffix);
				if (isPartial)
					name = name.Substring(0, name.Length - Common.PartialSuffix.Length);

				var isOnDemand = name.EndsWith(Common.OnDemandSuffix);
				if (isOnDemand)
					name = name.Substring(0, name.Length - Common.OnDemandSuffix.Length);

				var isAtBoot = name.EndsWith(Common.AtBootSuffix);
				if (isAtBoot)
					name = name.Substring(0, name.Length - Common.AtBootSuffix.Length);

				if (!DateTime.TryParseExact(name, Common.BackupFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
					continue;  // directory name does not match backup format, skip

				var type = BackupType.OnDemand;
				if (!isOnDemand)
				{
					// only the first backup for each period counts
					var isYearly = backups.Find(date, BackupType.Yearly) == null;
					var isMonthly = backups.Find(date, BackupType.Monthly) == null;
					var isWeekly = backups.Find(date, BackupType.Weekly, Config.FirstDayOfWeek) == null;
					var isDaily = backups.Find(date, BackupType.Daily) == null;
					var isHourly = backups.Find(date, BackupType.Hourly) == null;

					type = (
						default(BackupType)
						| (isAtBoot ? BackupType.AtBoot : 0)
						| (isYearly ? BackupType.Yearly : 0)
						| (isMonthly ? BackupType.Monthly : 0)
						| (isWeekly ? BackupType.Weekly : 0)
						| (isDaily ? BackupType.Daily : 0)
						| (isHourly ? BackupType.Hourly : 0)
					);
				}

				var backup = new BackupInfo(directory, date, type)
				{
					IsPartial = isPartial,
				};

				backups.Add(backup);
			}

			return Task.FromResult(backups);
		}
	}
}

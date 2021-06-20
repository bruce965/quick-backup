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
			await Console.Out.WriteLineAsync("Cleaning old backups...");

			var backups = await GetBackupsSetAsync();

			// TODO
			await Console.Error.WriteLineAsync("Not implemented, backups will not be cleaned.");

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

				var isOnDemand = name.EndsWith(Common.OnDemandSuffix);
				if (isOnDemand)
					name = name.Substring(0, name.Length - Common.OnDemandSuffix.Length);

				var isAtBoot = name.EndsWith(Common.AtBootSuffix);
				if (isAtBoot)
					name = name.Substring(0, name.Length - Common.AtBootSuffix.Length);

				if (!DateTime.TryParseExact(name, Common.BackupFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
					continue;  // directory name does not match backup format, skip

				// only the first backup for each period counts
				var isYearly = backups.Find(date, BackupType.Yearly, Config.FirstDayOfWeek) == null;
				var isMonthly = backups.Find(date, BackupType.Monthly, Config.FirstDayOfWeek) == null;
				var isWeekly = backups.Find(date, BackupType.Weekly, Config.FirstDayOfWeek) == null;
				var isDaily = backups.Find(date, BackupType.Daily, Config.FirstDayOfWeek) == null;
				var isHourly = backups.Find(date, BackupType.Hourly, Config.FirstDayOfWeek) == null;

				var type = (
					default(BackupType)
					| (isAtBoot ? BackupType.AtBoot : 0)
					| (isYearly ? BackupType.Yearly : 0)
					| (isMonthly ? BackupType.Monthly : 0)
					| (isWeekly ? BackupType.Weekly : 0)
					| (isDaily ? BackupType.Daily : 0)
					| (isHourly ? BackupType.Hourly : 0)
				);

				backups.Add(new BackupInfo(directory, date, type));
			}

			return Task.FromResult(backups);
		}
	}
}

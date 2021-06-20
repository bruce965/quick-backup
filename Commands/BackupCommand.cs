using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuickBackup.Config;
using QuickBackup.Data;
using QuickBackup.Logic;
using QuickBackup.Utility;

namespace QuickBackup.Commands
{
	public class BackupCommand : Command
	{
		public BackupCommand()
			: base("backup")
		{
			Description = "Perform a backup";

			Add(new Option<bool>(
				aliases: new[] { "--dry-run", "-n" },
				description: "Print list of operations without actually altering the file-system"));

			Add(new Option<bool>(
				aliases: new[] { "--on-demand", "-d" },
				description: "Make an unscheduled backup copy without counting it towards the configured limits"));

			Add(new Argument<string>
			{
				Name = "path",
				Description = $"Path of a configuration file, or the directory containing {Common.DefaultTargetDirectory}/{Common.DefaultConfigFile}",
				Arity = ArgumentArity.ZeroOrOne,
			});

			Handler = CommandHandler.Create<bool, bool, string>(Run);
		}

		public static async Task Run(bool dryRun, bool onDemand, string path)
		{
			if (string.IsNullOrEmpty(path))
				path = ".";  // no path specified? use current working directory

			if (Directory.Exists(path))
				path = Path.Combine(path, Common.DefaultTargetDirectory, Common.DefaultConfigFile);

			var config = await JsonUtil.ReadFileAsync<BackupConfig>(path);

			await ProcessAll(config, Directory.GetParent(path)!.ToString(), dryRun, onDemand);
		}

		public static async Task ProcessAll(BackupConfig config, string path, bool dryRun, bool onDemand)
		{
			await Console.Out.WriteLineAsync($"Backing up {config.Backups.Count} path(s)...{(dryRun ? " (DRY-RUN)" : "")}");

			foreach (var backup in config.Backups)
			{
				var manager = new BackupsManager(path, config, backup)
				{
					DryRun = dryRun,
				};

				await ProcessOne(manager, onDemand);
			}
		}

		public static async Task ProcessOne(BackupsManager manager, bool onDemand)
		{
			var sourcePath = Path.Combine(manager.BasePath, manager.Backup.SourcePath.ToString());
			var targetPath = Path.Combine(manager.BasePath, manager.Backup.TargetPath.ToString());

			var exclude = new HashSet<string>
			{
				Path.GetRelativePath(sourcePath, manager.BasePath),
			};

			var backups = await manager.GetBackupsSetAsync();
			var latestBackupPath = backups.Latest?.Path.ToString();  // note: may be a partial backup

			var date = DateTime.Now;

			if (onDemand)
			{
				var destinationPath = Path.Combine(targetPath, date.ToString(Common.BackupFormat, CultureInfo.InvariantCulture) + Common.OnDemandSuffix);

				var backup = await DoBackup(
					sourcePath,
					latestBackupPath,
					destinationPath,
					date,
					BackupType.OnDemand,
					exclude,
					manager.Backup.UseHardLinks,
					manager.Backup.FastCompare,
					manager.DryRun);

				backups.Add(backup);
			}
			else
			{
				var isAtBoot = manager.Backup.AtBootCount > 0 && backups.Find(date, BackupType.AtBoot, allowPartial: true) == null;
				var isYearly = manager.Backup.YearlyCount > 0 && backups.Find(date, BackupType.Yearly, allowPartial: true) == null;
				var isMonthly = manager.Backup.MonthlyCount > 0 && backups.Find(date, BackupType.Monthly, allowPartial: true) == null;
				var isWeekly = manager.Backup.WeeklyCount > 0 && backups.Find(date, BackupType.Weekly, manager.Config.FirstDayOfWeek, allowPartial: true) == null;
				var isDaily = manager.Backup.DailyCount > 0 && backups.Find(date, BackupType.Daily, allowPartial: true) == null;
				var isHourly = manager.Backup.HourlyCount > 0 && backups.Find(date, BackupType.Hourly, allowPartial: true) == null;

				var type = (
					default(BackupType)
					| (isAtBoot ? BackupType.AtBoot : 0)
					| (isYearly ? BackupType.Yearly : 0)
					| (isMonthly ? BackupType.Monthly : 0)
					| (isWeekly ? BackupType.Weekly : 0)
					| (isDaily ? BackupType.Daily : 0)
					| (isHourly ? BackupType.Hourly : 0)
				);

				if (type != default)
				{
					var destinationPath = Path.Combine(targetPath, date.ToString(Common.BackupFormat, CultureInfo.InvariantCulture) + (isAtBoot ? Common.AtBootSuffix : ""));

					var backup = await DoBackup(
						sourcePath,
						latestBackupPath,
						destinationPath,
						date,
						type,
						exclude,
						manager.Backup.UseHardLinks,
						manager.Backup.FastCompare,
						manager.DryRun);

					backups.Add(backup);
				}
				else
				{
					await Console.Out.WriteLineAsync($"Path '{sourcePath}' does not need a backup.");
				}
			}

			if (manager.Backup.AutoClean)
				await manager.CleanOldBackupsAsync();
		}

		public static async Task<BackupInfo> DoBackup(
			string source,
			string? latest,
			string destination,
			DateTime date,
			BackupType type,
			HashSet<string> exclude,
			bool useHardLinks,
			bool fastCompare,
			bool dryRun)
		{
			await Console.Out.WriteLineAsync($"Backing up from '{source}' to '{destination}'...{(dryRun ? " (DRY-RUN)" : "")}");
			await Console.Out.WriteLineAsync($"Backup type: {type}");
			await Console.Out.WriteLineAsync($"Latest backup path: {(latest == null ? "none" : $"'{latest}'")}");

			var unvisited = new List<FileSystemInfo>
			{
				new DirectoryInfo(source),
			};

			while (unvisited.Count > 0)
			{
				var fileSystemEntry = unvisited[unvisited.Count - 1];
				unvisited.RemoveAt(unvisited.Count - 1);

				var relativePath = Path.GetRelativePath(source, fileSystemEntry.FullName);
				var destinationPath = Path.Combine(destination + Common.PartialSuffix, relativePath);

				await Console.Out.WriteAsync($"'{relativePath}'");

				try
				{
					if (fileSystemEntry is DirectoryInfo sourceDirectory)
					{
						var destinationDirectory = new DirectoryInfo(destinationPath);

						if (exclude.Contains(relativePath))
						{
							await Console.Out.WriteLineAsync(" skipped (excluded)");
						}
						else
						{
							if (!dryRun)
								destinationDirectory.Create();  // async currently not supported

							unvisited.AddRange(sourceDirectory.EnumerateFileSystemInfos());  // async currently not supported

							await Console.Out.WriteLineAsync(" created");
						}
					}
					else if (fileSystemEntry is FileInfo sourceFile)
					{
						if (latest != null && useHardLinks)
						{
							var latestFile = new FileInfo(Path.Combine(latest, relativePath));

							var areEqual = latestFile.Exists && sourceFile.Length == latestFile.Length;
							if (areEqual)
							{
								if (fastCompare)
								{
									// just compare last modification date
									areEqual = sourceFile.LastWriteTimeUtc == latestFile.LastWriteTimeUtc;
								}
								else
								{
									// compare contents
									areEqual = await StreamComparer.AreFilesEqualAsync(sourceFile.FullName, latestFile.FullName);
								}
							}

							if (areEqual)
							{
								if (!dryRun)
									latestFile.HardLinkTo(destinationPath);  // async currently not supported

								await Console.Out.WriteLineAsync(" unchanged, hard-linked");
							}
							else
							{
								if (!dryRun)
									sourceFile.CopyTo(destinationPath);  // async currently not supported

								await Console.Out.WriteLineAsync(" changed, copied");
							}
						}
						else
						{
							if (!dryRun)
								sourceFile.CopyTo(destinationPath);  // async currently not supported

							await Console.Out.WriteLineAsync(" copied");
						}
					}
					else
					{
						await Console.Out.WriteLineAsync(" not supported");
					}
				}
				catch (Exception e)
				{
					await Console.Out.WriteLineAsync(" failed (error, see next line)");
					await Console.Error.WriteLineAsync(e.ToString());
				}
			}

			new DirectoryInfo(destination + Common.PartialSuffix).MoveTo(destination);  // async currently not supported

			await Console.Out.WriteLineAsync($"Done.");

			return new BackupInfo(new DirectoryInfo(destination), date, type);
		}
	}
}

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuickBackup.Config;
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
			await Console.Out.WriteLineAsync($"Backing up {config.Backups.Count} path(s) to '{path}'...{(dryRun ? " (DRY-RUN)" : "")}");

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
			var latestBackupPath = backups.LastOrDefault()?.Path.ToString();

			if (onDemand)
			{
				var destinationPath = Path.Combine(targetPath, DateTime.Now.ToString(Common.BackupFormat, CultureInfo.InvariantCulture) + Common.OnDemandSuffix);

				await DoBackup(
					sourcePath,
					latestBackupPath,
					destinationPath,
					exclude,
					manager.Backup.UseHardLinks,
					manager.Backup.FastCompare,
					manager.DryRun);
			}
			else
			{
				// TODO
				await Console.Error.WriteLineAsync("Not implemented, only on-demand backups are currently supported.");
			}

			await Console.Out.WriteLineAsync();
		}

		public static async Task DoBackup(
			string source,
			string? latest,
			string destination,
			HashSet<string> exclude,
			bool useHardLinks,
			bool fastCompare,
			bool dryRun)
		{
			await Console.Out.WriteLineAsync($"Backing up from '{source}' to '{destination}'...{(dryRun ? " (DRY-RUN)" : "")}");
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
				var destinationPath = Path.Combine(destination, relativePath);

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

			await Console.Out.WriteLineAsync($"Done.");
		}
	}
}

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using QuickBackup.Config;
using QuickBackup.Logic;
using QuickBackup.Utility;

namespace QuickBackup.Commands
{
	public class CleanCommand : Command
	{
		public CleanCommand()
			: base("clean")
		{
			Description = "Remove old backups";

			Add(new Option<bool>(
				aliases: new[] { "--dry-run", "-n" },
				description: "Print list of operations without actually altering the file-system"));

			Add(new Argument<string>
			{
				Name = "path",
				Description = $"Path of a configuration file, or the directory containing {Common.DefaultTargetDirectory}/{Common.DefaultConfigFile}",
				Arity = ArgumentArity.ZeroOrOne,
			});

			Handler = CommandHandler.Create<bool, string>(Run);
		}

		public static async Task Run(bool dryRun, string path)
		{
			if (string.IsNullOrEmpty(path))
				path = ".";  // no path specified? use current working directory

			if (Directory.Exists(path))
				path = Path.Combine(path, Common.DefaultTargetDirectory, Common.DefaultConfigFile);

			var config = await JsonUtil.ReadFileAsync<BackupConfig>(path);

			await ProcessAll(config, Directory.GetParent(path)!.ToString(), dryRun);
		}

		public static async Task ProcessAll(BackupConfig config, string path, bool dryRun)
		{
			await Console.Out.WriteLineAsync($"Cleaning old backups at {config.Backups.Count} path(s)...{(dryRun ? " (DRY-RUN)" : "")}");

			foreach (var backup in config.Backups)
			{
				var manager = new BackupsManager(path, config, backup)
				{
					DryRun = dryRun,
				};

				await manager.CleanOldBackupsAsync();
			}
		}
	}
}

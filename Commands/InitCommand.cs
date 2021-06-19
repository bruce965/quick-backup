using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using QuickBackup.Config;
using QuickBackup.Utility;

namespace QuickBackup.Commands
{
	public class InitCommand : Command
	{
		public InitCommand()
			: base("init")
		{
			Description = "Create a configuration file";

			Add(new Option<bool>(
				aliases: new[] { "--yes", "-y" },
				description: "Generate example configuration file without asking questions"));

			Handler = CommandHandler.Create<bool>(Run);
		}

		public static async Task Run(bool yes)
		{
			var config = new BackupConfig();

			var backup = new Backup();

			backup.SourcePath = CommandLineUtil.AskValue(
				name: "source",
				description: "Which directory would you like to create a backup for?",
				autoConfirm: yes,
				fallback: new DirectoryInfo("./"));

			var targetPath = new DirectoryInfo(Path.Combine(backup.SourcePath.ToString(), Common.DefaultTargetDirectory).Replace(Path.DirectorySeparatorChar, '/'));
			backup.TargetPath = CommandLineUtil.AskValue(
				name: "target",
				description: "Where would you like to store your backups?",
				autoConfirm: yes,
				fallback: targetPath);

			backup.AtBootCount = CommandLineUtil.AskValue(
				name: "boot",
				description: "How many boot-time backups would you like to keep?",
				autoConfirm: yes,
				fallback: 0);

			backup.YearlyCount = CommandLineUtil.AskValue(
				name: "yearly",
				description: "How many yearly backups would you like to keep?",
				autoConfirm: yes,
				fallback: 0);

			backup.MonthlyCount = CommandLineUtil.AskValue(
				name: "monthly",
				description: "How many monthly backups would you like to keep?",
				autoConfirm: yes,
				fallback: 5);

			backup.WeeklyCount = CommandLineUtil.AskValue(
				name: "weekly",
				description: "How many weekly backups would you like to keep?",
				autoConfirm: yes,
				fallback: 0);

			backup.DailyCount = CommandLineUtil.AskValue(
				name: "daily",
				description: "How many daily backups would you like to keep?",
				autoConfirm: yes,
				fallback: 5);

			backup.HourlyCount = CommandLineUtil.AskValue(
				name: "hourly",
				description: "How many hourly backups would you like to keep?",
				autoConfirm: yes,
				fallback: 0);

			backup.UseHardLinks = CommandLineUtil.AskValue(
				name: "link",
				description: "Would you like to use hard links for subsequent backups instead of copying all files again (will save space)?",
				autoConfirm: yes,
				fallback: true);

			if (backup.UseHardLinks)
			{
				backup.FastCompare = CommandLineUtil.AskValue(
					name: "fast",
					description: "Would you like to just compare file size and last modification date to detect changes (will improve performance)?",
					autoConfirm: yes,
					fallback: true);
			}

			config.Backups.Add(backup);

			var configPath = new FileInfo(Path.Combine(backup.TargetPath.ToString(), Common.DefaultConfigFile).Replace(Path.DirectorySeparatorChar, '/'));
			while (true)
			{
				configPath = CommandLineUtil.AskValue(
					name: null,
					description: "Where would you like to save your configuration?",
					autoConfirm: yes,
					fallback: configPath);

				if (configPath.Exists)
				{
					var confirm = CommandLineUtil.AskValue(
						name: null,
						description: "File already exists, are you sure?",
						autoConfirm: false,  // going for the default choice, this might cause issues
						fallback: false);

					if (!confirm)
					{
						yes = false;  // disable auto-confirm
						continue;
					}
				}

				break;
			}

			var configDirectory = configPath.Directory!;
			backup.SourcePath = new DirectoryInfo(Path.GetRelativePath(configDirectory.ToString(), backup.SourcePath.ToString()).Replace(Path.DirectorySeparatorChar, '/'));
			backup.TargetPath = new DirectoryInfo(Path.GetRelativePath(configDirectory.ToString(), backup.TargetPath.ToString()).Replace(Path.DirectorySeparatorChar, '/'));

			configDirectory.Create();
			await JsonUtil.WriteFileAsync(configPath.ToString(), config);

			Console.WriteLine("Configuration file generated.");
		}
	}
}

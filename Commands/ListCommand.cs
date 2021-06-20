using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using QuickBackup.Config;
using QuickBackup.Logic;
using QuickBackup.Utility;
using static System.CommandLine.Rendering.Ansi.Color;

namespace QuickBackup.Commands
{
	public class ListCommand : Command
	{
		public ListCommand()
			: base("list")
		{
			Description = "View list of backups";

			Add(new Argument<string>
			{
				Name = "path",
				Description = $"Path of a configuration file, or the directory containing {Common.DefaultTargetDirectory}/{Common.DefaultConfigFile}",
				Arity = ArgumentArity.ZeroOrOne,
			});

			Handler = CommandHandler.Create<string>(Run);
		}

		public static async Task Run(string path)
		{
			if (string.IsNullOrEmpty(path))
				path = ".";  // no path specified? use current working directory

			if (Directory.Exists(path))
				path = Path.Combine(path, Common.DefaultTargetDirectory, Common.DefaultConfigFile);

			var config = await JsonUtil.ReadFileAsync<BackupConfig>(path);

			await PrintAll(config, Directory.GetParent(path)!.ToString());
		}

		public static async Task PrintAll(BackupConfig config, string path)
		{

			foreach (var backup in config.Backups)
			{
				var manager = new BackupsManager(path, config, backup)
				{
					DryRun = true,  // this is supposedly a read-only command, better avoid mistakes
				};

				var targetPath = Path.Combine(manager.BasePath, manager.Backup.TargetPath.ToString());

				await Console.Out.WriteLineAsync($"Backups at '{targetPath}':");

				var allBackups = await manager.GetBackupsSetAsync();
				var oldBackups = await manager.GetOldBackupsListAsync();

				foreach (var info in allBackups)
				{
					var toBeRemoved = oldBackups.Contains(info);

					await Console.Out.WriteLineAsync($"{info.Path.Name} ({info.Type}){(info.IsPartial ? $" {Foreground.LightRed}[PARTIAL]{Foreground.Default}" : "")}{(toBeRemoved ? $" {Foreground.LightYellow}[SHOULD CLEAN]{Foreground.Default}" : "")}");
				}
			}
		}
	}
}

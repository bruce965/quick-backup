using System.CommandLine;
using System.Threading.Tasks;
using QuickBackup.Commands;

namespace QuickBackup
{
	class Program
	{
		static Task<int> Main(string[] args)
		{
			var rootCommand = new RootCommand
			{
				Description = "Create backups, quick and easy.",
			};

			rootCommand.Add(new InitCommand());
			rootCommand.Add(new BackupCommand());

			return rootCommand.InvokeAsync(args);
		}
	}
}

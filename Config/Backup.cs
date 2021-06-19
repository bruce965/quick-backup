using System.IO;
using System.Text.Json.Serialization;

namespace QuickBackup.Config
{
	public class Backup
	{
		[JsonPropertyName("source")]
		public DirectoryInfo SourcePath { get; set; }
			= new DirectoryInfo(".");

		[JsonPropertyName("target")]
		public DirectoryInfo TargetPath { get; set; }
			= new DirectoryInfo(Common.DefaultTargetDirectory);

		[JsonPropertyName("boot")]
		public int AtBootCount { get; set; }

		[JsonPropertyName("yearly")]
		public int YearlyCount { get; set; }

		[JsonPropertyName("monthly")]
		public int MonthlyCount { get; set; }

		[JsonPropertyName("weekly")]
		public int WeeklyCount { get; set; }

		[JsonPropertyName("daily")]
		public int DailyCount { get; set; }

		[JsonPropertyName("hourly")]
		public int HourlyCount { get; set; }

		[JsonPropertyName("link")]
		public bool UseHardLinks { get; set; }

		[JsonPropertyName("fast")]
		public bool FastCompare { get; set; }
	}
}

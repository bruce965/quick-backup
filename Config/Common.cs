namespace QuickBackup.Config
{
	public static class Common
	{
		public static string DefaultTargetDirectory
			=> ".backups";

		public static string DefaultConfigFile
			=> "backup-config.json";

		public static string BackupFormat
			=> "yyyy-MM-dd_HH-mm-ss";

		public static string OnDemandSuffix
			=> "_ondemand";

		public static string AtBootSuffix
			=> "_boot";
	}
}

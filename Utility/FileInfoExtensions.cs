using System.IO;

namespace QuickBackup.Utility
{
	public static class FileInfoExtensions
	{
		public static void HardLinkTo(this FileInfo fileInfo, string destFileName)
			=> HardLink.Create(fileInfo.ToString(), destFileName);
	}
}

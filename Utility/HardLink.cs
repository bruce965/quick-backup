using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace QuickBackup.Utility
{
	public static class HardLink
	{
		static class Windows
		{
			// https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d
			enum ErrorCode
			{
				ERROR_PATH_NOT_FOUND = 3,
				ERROR_ACCESS_DENIED = 5,
			}

			// https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createhardlinkw
			[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
			static extern bool CreateHardLinkW(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

			public static void Link(string oldName, string newName)
			{
				// TODO: more specific exceptions.
				if (!CreateHardLinkW(newName, oldName, IntPtr.Zero))
				{
					var errorCode = Marshal.GetLastWin32Error();
					var ex = new Win32Exception(errorCode);
					
					throw (ErrorCode)errorCode switch
					{
						ErrorCode.ERROR_PATH_NOT_FOUND => new FileNotFoundException("The system cannot find the path specified.", ex),
						ErrorCode.ERROR_ACCESS_DENIED => new UnauthorizedAccessException("Access is denied.", ex),
						_ => new IOException($"Unexpected error {errorCode}: {ex.Message}", ex)
					};
				}
			}
		}

		static class Linux
		{
			enum Errno
			{
				EPERM = 1,
				ENOENT = 2,
				EIO = 5,
				EACCES = 13,
				EEXIST = 17,
				EXDEV = 18,
				ENOTDIR = 20,
				ENOSPC = 28,
				EROFS = 30,
				EMLINK = 31,
				ENAMETOOLONG = 36,
				ELOOP = 40,
			}

			// https://www.gnu.org/software/libc/manual/html_node/Hard-Links.html#Hard-Links
			[DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi)]
			static extern int link(string oldname, string newname);

			public static void Link(string oldName, string newName)
			{
				if (link(oldName, newName) != 0)
				{
					var errno = Marshal.GetLastWin32Error();  // deceptive name, but it works on Linux too
					var ex = new Win32Exception(errno);

					throw (Errno)errno switch
					{
						Errno.EPERM => new UnauthorizedAccessException("Operation not permitted.", ex),
						Errno.ENOENT => new FileNotFoundException("No such file or directory.", ex),
						Errno.EIO => new IOException("Input/output error.", ex),
						Errno.EACCES => new UnauthorizedAccessException("Permission denied.", ex),
						Errno.EEXIST => new IOException("File already exists.", ex),
						Errno.EXDEV => new IOException("Invalid cross-device link.", ex),
						Errno.ENOTDIR => new IOException("Not a directory.", ex),
						Errno.ENOSPC => new IOException("No space left on device.", ex),
						Errno.EROFS => new IOException("Read-only file system.", ex),
						Errno.EMLINK => new IOException("Too many links.", ex),
						Errno.ENAMETOOLONG => new PathTooLongException("File name too long.", ex),
						Errno.ELOOP => new IOException("Too many levels of symbolic links.", ex),
						_ => new IOException($"Unexpected error {errno}: {ex.Message}", ex)
					};
				}
			}
		}

		static Action<string, string>? create = null;

		public static void Create(string sourceFileName, string destFileName)
		{
			if (create == null)
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					create = Windows.Link;
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					create = Linux.Link;
				else
					create = (_, _) => throw new NotImplementedException($"Hard linking support not implemented for the current platform: {RuntimeInformation.RuntimeIdentifier}");
			}
			
			create(sourceFileName, destFileName);
		}
	}
}

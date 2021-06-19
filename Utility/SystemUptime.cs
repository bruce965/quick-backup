using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace QuickBackup.Utility
{
	public static class SystemUptime
	{
		static class Windows
		{
			// https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-gettickcount64
			[DllImport("kernel32")]
			static extern long GetTickCount64();

			public static TimeSpan GetUptime()
				=> TimeSpan.FromTicks(GetTickCount64());
		}

		static class Linux
		{
			public static TimeSpan GetUptime()
				=> TimeSpan.FromSeconds(double.Parse(File.ReadAllText("/proc/uptime").Split(" ")[0], CultureInfo.InvariantCulture));
		}

		static Func<TimeSpan>? getUptime = null;

		public static TimeSpan GetUptime()
		{
			if (getUptime == null)
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					getUptime = Windows.GetUptime;
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					getUptime = Linux.GetUptime;
				else
					getUptime = () => throw new NotImplementedException($"Uptime support not implemented for the current platform: {RuntimeInformation.RuntimeIdentifier}");
			}
			
			return getUptime();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using static System.CommandLine.Rendering.Ansi;
using static System.CommandLine.Rendering.Ansi.Color;

namespace QuickBackup.Utility
{
	public static class CommandLineUtil
	{
		static readonly ISet<string> BoolFalse = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"no", "n", "false", "f", "0",
		};

		static readonly ISet<string> BoolTrue = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"yes", "y", "true", "t", "1",
		};

		[return: NotNullIfNotNull("fallback")]
		public static T? AskValue<T>(
			string? name,
			string? description,
			bool allowNull = false,
			bool autoConfirm = false,
			T? fallback = default)
		{
			while (true)
			{
				var fallbackRaw = typeof(T) switch
				{
					Type t when t.IsAssignableFrom(typeof(bool)) && fallback != null => true.Equals(fallback) ? "yes" : "no",
					_ => Convert.ToString(fallback, CultureInfo.InvariantCulture)
				};

				if (description != null)
					Console.WriteLine($"{Foreground.LightGreen}>{Foreground.Default} {description}");

				if (!string.IsNullOrEmpty(name))
					Console.Write($"{Foreground.LightYellow}{name}{Foreground.Default}");

				if (fallback != null)
				{
					if (!string.IsNullOrEmpty(name))
						Console.Write(" ");

					Console.Write($"[default: {fallbackRaw}]");
				}

				Console.Write($": ");

				string? valueRaw;
				if (autoConfirm)
				{
					valueRaw = "";
					Console.WriteLine();
				}
				else
				{
					valueRaw = Console.ReadLine();
				}

				if (string.IsNullOrEmpty(valueRaw))
					return fallback;

				try
				{
					var value = typeof(T) switch
					{
						Type t when typeof(FileSystemInfo).IsAssignableFrom(t) => Activator.CreateInstance(t, valueRaw),
						Type t when typeof(bool).IsAssignableFrom(t) => BoolTrue.Contains(valueRaw) || (BoolFalse.Contains(valueRaw) ? false : throw new FormatException()),
						_ => Convert.ChangeType(valueRaw, typeof(T), CultureInfo.InvariantCulture)
					};

					if (value != null || allowNull)
						return (T?)value;
				}
				catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
				{
					Console.WriteLine("Invalid value.");
					autoConfirm = false;
				}
			}
		}
	}
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuickBackup.Utility
{
	public static class StreamComparer
	{
		public static Task<bool> AreFilesEqualAsync(string fileA, string fileB, CancellationToken cancellationToken = default)
		{
			using var streamA = File.OpenRead(fileA);
			using var streamB = File.OpenRead(fileB);

			if (streamA.Length != streamB.Length)
				return Task.FromResult(false);

			return AreEqualAsync(streamA, streamB, cancellationToken);
		}

		public static async Task<bool> AreEqualAsync(Stream streamA, Stream streamB, CancellationToken cancellationToken = default)
		{
			var bufferA = new byte[4096];
			var bufferB = new byte[4096];

			while (true)
			{
				var readA = await streamA.ReadAsync(bufferA.AsMemory(0, 4096), cancellationToken);
				if (readA == 0)
				{
					var read = await streamB.ReadAsync(bufferB.AsMemory(0, 1), cancellationToken);
					if (read == 0)
						return true;  // same length and same data

					return false;  // different length (A is shorter than B)
				}

				var readB = 0;
				do
				{
					var read = await streamB.ReadAsync(bufferB.AsMemory(readB, readA), cancellationToken);
					readB += read;

					if (readB == 0)
						return false;  // different length (A is longer than B)
				}
				while (readB < readA);

				for (var i = 0; i < readA; i++)
					if (bufferA[i] != bufferB[i])
						return false;  // different data
			}
		}
	}
}

using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QuickBackup.Utility.Json;

namespace QuickBackup.Utility
{
	public static class JsonUtil
	{
		public static JsonSerializerOptions SerializerOptions { get; } = new()
		{
			WriteIndented = true,
			Converters =
			{
				FileSystemInfoJsonConverter.Instance,
			},
		};

		public static async Task<T> ReadFileAsync<T>(string filePath, CancellationToken cancellationToken = default)
		{
			using var stream = File.OpenRead(filePath);

			var data = await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
			if (data == null)
				throw new JsonException();

			return data;
		}

		public static async Task WriteFileAsync<T>(string filePath, T data, CancellationToken cancellationToken = default)
		{
			using var stream = File.OpenWrite(filePath);

			await JsonSerializer.SerializeAsync(stream, data, SerializerOptions, cancellationToken);

			// truncate (in case file already existed and was longer)
			stream.SetLength(stream.Position);

			await stream.FlushAsync(cancellationToken);
		}
	}
}

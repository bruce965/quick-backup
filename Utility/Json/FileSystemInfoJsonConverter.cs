using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickBackup.Utility.Json
{
	public class FileSystemInfoJsonConverter : JsonConverter<FileSystemInfo>
	{
		public static FileSystemInfoJsonConverter Instance { get; }
			= new();

		public override bool CanConvert(Type typeToConvert)
			=> typeof(FileSystemInfo).IsAssignableFrom(typeToConvert);

		public override FileSystemInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> (FileSystemInfo?)Activator.CreateInstance(typeToConvert, reader.GetString());

		public override void Write(Utf8JsonWriter writer, FileSystemInfo value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToString());
	}
}

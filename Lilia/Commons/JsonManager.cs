using System;
using System.IO;
using System.Reflection;
using Lilia.Json;
using Newtonsoft.Json;
using Serilog;

namespace Lilia.Commons;

public static class JsonManager<T> where T : BaseJson
{
	private static void EnsureFileExists(string filePath, bool createIfNotExist = true)
	{
		if (File.Exists(filePath)) return;

		if (createIfNotExist)
		{
			File.Create(filePath!).Dispose();
			File.WriteAllText(filePath, JsonConvert.SerializeObject((T)Activator.CreateInstance(typeof(T)), Formatting.Indented));
			Log.Logger.Warning("Created JSON file in path {FilePath} since one doesn't exist", filePath);
		}
		else
		{
			Log.Fatal("{FilePath} not found and not created", filePath);
			throw new FileNotFoundException("Required JSON file does not exist", filePath);
		}
	}

	public static T Read()
	{
		try
		{
			// https://docs.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.invoke?view=net-6.0
			// https://stackoverflow.com/questions/8413524/how-to-get-an-overloaded-private-protected-method-using-reflection/8413652
			var type = typeof(T);
			var constructor = type.GetConstructor(Type.EmptyTypes);
			var obj = constructor?.Invoke(null);
			var getter = type.GetMethod("GetFilePath", BindingFlags.NonPublic | BindingFlags.Instance);
			var filePath = getter?.Invoke(obj, null)?.ToString();

			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentException("Invalid file path", nameof(filePath));

			EnsureFileExists(filePath);
			return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
		}
		catch (NullReferenceException)
		{
			Log.Logger.Fatal("Error in reading JSON, maybe you did not set a constructor with FilePath field overridden");
			throw;
		}
	}
}

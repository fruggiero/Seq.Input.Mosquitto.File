using System.IO.Abstractions.TestingHelpers;
using System.Reflection;

namespace Seq.Input.Mosquitto.File.Test;

public class Helper
{
    public static string LoadConfigFromResource(MockFileSystem fs, string path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.Static.{path.Replace("\\", ".")}";
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        using (var reader = new StreamReader(stream!))
        {
            var result = reader.ReadToEnd();
            fs.AddFile(fs.Path.Combine(@"C:\", fs.Path.GetFileName(path)), result);
        }

        return fs.Path.Combine(@"C:\", fs.Path.GetFileName(path));
    }

    public static string LoadConfigFromResource(MockFileSystem fs, string path, string directory)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.Static.{path.Replace("\\", ".")}";
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        using (var reader = new StreamReader(stream!))
        {
            var result = reader.ReadToEnd();
            fs.AddFile(fs.Path.Combine(@"C:\", directory, fs.Path.GetFileName(path)), result);
        }

        return fs.Path.Combine(@"C:\", directory, fs.Path.GetFileName(path));
    }
}
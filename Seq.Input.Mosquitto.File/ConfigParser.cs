using System.IO.Abstractions;

namespace Seq.Input.Mosquitto.File
{
    public class ConfigParser
    {
        private readonly IFileSystem _fs;

        public ConfigParser(IFileSystem fs)
        {
            _fs = fs;
        }

        public LogConfig Parse(string filepath)
        {
            var result = new LogConfig();
            using (var reader = _fs.File.OpenText(filepath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if(line.TrimStart().StartsWith("#"))
                        continue;
                    if (line.Trim().Length <= 0) continue;

                    var directive = ParseDirective(line);
                    switch (directive.Name)
                    {
                        case "log_dest":
                            var logPath = ParseLogDestFilePath(directive);
                            result.LogFilePath = logPath;
                            break;
                        case "log_timestamp_format":
                            result.Format = line.Substring(line.IndexOf(' ') + 1, line.Length - line.IndexOf(' ') - 1);
                            break;
                    }
                }
            }
            return result;
        }

        private static (string Name, string Attributes) ParseDirective(string line)
        {
            var directive = line.TrimStart();
            var clean = directive;
            directive = directive.Substring(0, directive.IndexOf(' '));

            return (directive,
                clean.Substring(directive.Length, clean.Length - directive.Length).TrimStart());
        }

        private static string ParseLogDestFilePath((string name, string attributes) directive)
        {
            if (!directive.attributes.StartsWith("file "))
            {
                throw new InvalidConfigException("Only 'log_dest file' directives are supported");
            }

            return directive.attributes
                .Substring("file".Length, directive.attributes.Length - "file".Length).TrimStart();
        }
    }
}
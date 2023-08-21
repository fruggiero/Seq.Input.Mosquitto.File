using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Timers;
using Newtonsoft.Json;
using Seq.Apps;
// ReSharper disable All

namespace Seq.Input.Mosquitto.File
{
    using File = System.IO.File;

    [SeqApp("Eclipse Mosquitto Input", Description = "Pulls from Eclipse Mosquitto log file")]
    public class MosquittoInput : SeqApp, IPublishJson
    {
        [SeqAppSetting(
            DisplayName = "Log file path",
            HelpText = "The full path to Mosquitto log file",
            InputType = SettingInputType.Text)]
        public string LogFilePath { get; set; }

        [SeqAppSetting(
            DisplayName = "Config path",
            HelpText = "The full path to Mosquitto config file. \nIf you use an external config file, with the " +
                       "include_dir directive, and this file contains logging configuration (like, for example, the " +
                       "'log_dest' directive), use this path instead",
            InputType = SettingInputType.Text)]
        public string ConfigPath { get; set; }
        
        private long _lastSize;
        private readonly Timer _timer = new Timer(500);
        private TextWriter _inputWriter;
        
        
        public MosquittoInput()
        {
            _timer.Elapsed += TimerOnElapsed;
        }

        public void Start(TextWriter inputWriter)
        {
            if (LogFilePath == null) throw new ArgumentException("LogFilePath cannot be null");
            
            var cacheSizeFile = Path.Combine(App.StoragePath, "lastSize.txt");
            if (File.Exists(cacheSizeFile))
            {
                _lastSize = long.Parse(File.ReadAllText(cacheSizeFile), CultureInfo.InvariantCulture);
            }
            else
            {
                File.WriteAllText(cacheSizeFile, _lastSize.ToString(CultureInfo.InvariantCulture));
            }

            _inputWriter = inputWriter;
            _timer.Start();
            base.Log.Information($"CacheSizeFile: {cacheSizeFile}");
        }

        public void Stop()
        {
            _timer.Stop();
            _inputWriter = null;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    _lastSize = 0;
                    var cacheSizeFile = Path.Combine(App.StoragePath, "lastSize.txt");
                    File.WriteAllText(cacheSizeFile, _lastSize.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                using (var fs = File.Open(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var toRead = fs.Length - _lastSize;
                    _lastSize = fs.Length;
                    if (toRead == 0) return;
                    fs.Seek(-toRead, SeekOrigin.End);
                    var bytes = new byte[toRead];
                    _ = fs.Read(bytes, 0, bytes.Length);
                    var str = Encoding.Default.GetString(bytes);
                    var lines = str.Split('\n');

                    foreach (var line in lines)
                    {
                        var expando = new ExpandoObject() as IDictionary<string, object>;
                        if (line.Trim().Length < 1) continue;
                        var args = line.Split(':');
                        expando["@t"] = DateTimeOffset.FromUnixTimeSeconds(long.Parse(args[0].Trim())).UtcDateTime
                            .ToString("o");
                        expando["@mt"] = args[1];
                        _inputWriter.WriteLineAsync(JsonConvert.SerializeObject(expando));
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, "UnauthorizedAccessException during Mosquitto log file parsing");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unknown error");
            }
        }
    }
}
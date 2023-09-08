using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using Newtonsoft.Json;
using Seq.Apps;
using StrftimeParser;

// ReSharper disable All

namespace Seq.Input.Mosquitto.File
{
    
    [SeqApp("Eclipse Mosquitto Input", Description = "Pulls from Eclipse Mosquitto log file")]
    public class MosquittoInput : SeqApp, IPublishJson
    {
        [SeqAppSetting(
            DisplayName = "Log file path",
            HelpText = "The full path to Mosquitto log file.",
            InputType = SettingInputType.Text)]
        public string LogFilePath { get; set; }

        [SeqAppSetting(
            DisplayName = "Config path",
            HelpText = "The full path of the configuration file, which contains the log_dest and log_timestamp_format directives.",
            InputType = SettingInputType.Text)]
        public string ConfigPath { get; set; }

        private readonly IScheduler _scheduler;
        private readonly IFileSystem _fs;
        private readonly IConfigParser _configParser;
        private IAppProvider _appProvider;
        private ILoggerProvider _loggerProvider;
        private readonly IObservable<long> _observable;
        private readonly IObserver<long> _observer;
        private IDisposable _subscription;
        private LogConfig _config;
        private TextWriter _inputWriter;
        private long _lastSize;
        private string _cacheFilePath;

        protected override void OnAttached()
        {
            _appProvider = new AppProvider(App);
            _loggerProvider = new LoggerProvider(Log);
            base.OnAttached();
        }

        public MosquittoInput()
        {
            _scheduler = new NewThreadScheduler();
            _fs = new FileSystem();
            _configParser = new ConfigParser(_fs);
            _observer = new AnonymousObserver<long>(OnTick);
            _observable = Observable.Interval(TimeSpan.FromMilliseconds(500), _scheduler);
        }

        internal MosquittoInput(IScheduler scheduler, IFileSystem fs, IConfigParser configParser, IAppProvider appProvider, ILoggerProvider loggerProvider)
        {
            _scheduler = scheduler;
            _fs = fs;
            _configParser = configParser;
            _appProvider = appProvider;
            _loggerProvider = loggerProvider;
            _observer = new AnonymousObserver<long>(OnTick);
            _observable = Observable.Interval(TimeSpan.FromMilliseconds(500), _scheduler);
        }

        public void Start(TextWriter inputWriter)
        {
            _cacheFilePath =  _fs.Path.Combine(_appProvider.App.StoragePath, "lastSize.txt");

            if (LogFilePath == null) throw new ArgumentException("LogFilePath cannot be null");
            if (ConfigPath == null) throw new ArgumentException("ConfigPath cannot be null");

            _config = _configParser.Parse(ConfigPath);
            
            if (_fs.File.Exists(_cacheFilePath))
            {
                _lastSize = long.Parse(_fs.File.ReadAllText(_cacheFilePath), CultureInfo.InvariantCulture);
            }
            else
            {
                _fs.File.WriteAllText(_cacheFilePath, _lastSize.ToString(CultureInfo.InvariantCulture));
            }

            _inputWriter = inputWriter;
            _subscription = _observable.Subscribe(_observer);
            _loggerProvider.Log.Information($"CacheSizeFile: {_cacheFilePath}");
        }

        public void Stop()
        {
            _subscription.Dispose();
            _inputWriter = null;
        }

        private void OnTick(long obj)
        {
            try
            {
                if (!_fs.File.Exists(LogFilePath))
                {
                    _lastSize = 0;
                    _fs.File.WriteAllText(_cacheFilePath, _lastSize.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                using (var fs = _fs.File.Open(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                        DateTime dtPart;
                        try
                        {
                            dtPart = Strftime.Parse(line, _config.Format);
                        }
                        catch (Exception ex)
                        {
                            _loggerProvider.Log.Error(ex, "Parsing error");
                            continue;
                        }

                        var len = Strftime.ToString(dtPart, _config.Format).Length;

                        var expando = new ExpandoObject() as IDictionary<string, object>;
                        if (line.Trim().Length < 1) continue;
                        expando["@t"] = dtPart.ToString("o");
                        expando["@mt"] = line.Substring(len + 2, line.Length - len - 2);
                        _inputWriter.WriteLineAsync(JsonConvert.SerializeObject(expando));
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggerProvider.Log.Error(ex, "UnauthorizedAccessException during Mosquitto log file parsing");
            }
            catch (Exception ex)
            {
                _loggerProvider.Log.Error(ex, "Unknown error");
            }
        }
    }
}
using System.Dynamic;
using System.Globalization;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Newtonsoft.Json;

using NSubstitute;
using Seq.Apps;
using StrftimeParser;

namespace Seq.Input.Mosquitto.File.Test;

public class MosquittoInputTest
{
    private readonly MockFileSystem _fs;
    private readonly TestScheduler _testScheduler;
    private readonly TextWriter _writer;
    private readonly IConfigParser _configParser;
    private readonly IAppProvider _appProvider;
    private readonly ILoggerProvider _loggerProvider;
    private readonly string _logFilePath;
    private readonly MosquittoInput _sut;
    private readonly Fixture _fixture;
    private const string _format = "%Y-%m-%dT%H:%M:%S";
    
    public MosquittoInputTest()
    {
        _fixture = new Fixture();
        _fs = new MockFileSystem();
        _testScheduler = new TestScheduler();
        _writer = Substitute.For<TextWriter>();
        _configParser = Substitute.For<IConfigParser>();
        _appProvider = Substitute.For<IAppProvider>();
        _loggerProvider = Substitute.For<ILoggerProvider>();
        const string storagePath = @"D:\App";
        _fs.AddDirectory(storagePath);
        _appProvider.App.Returns(new App("", "", new Dictionary<string, string>(), storagePath));
        var configPath = Helper.LoadConfigFromResource(_fs, "mosquitto.conf");
        _logFilePath = @"D:\mosquitto.log";
        _configParser.Parse(configPath).Returns(new LogConfig
        {
            Format = _format,
            LogFilePath = _logFilePath
        });
        _sut = new MosquittoInput(_testScheduler, _fs, _configParser, _appProvider, _loggerProvider)
        {
            LogFilePath = _logFilePath,
            ConfigPath = configPath
        };
    }

    [Fact]
    public void Should_Start()
    {
        // arrange
        var dtString = "2023-08-07T17:47:09";
        var message = "Client clientName closed its connection.";
        _fs.AddFile(_sut.LogFilePath, $"{dtString}: {message}");
        var dt = Strftime.Parse(dtString, _format, CultureInfo.InvariantCulture);
        var expando = new ExpandoObject() as IDictionary<string, object>;
        expando["@t"] = dt.ToString("o");
        expando["@mt"] = message;

        // act
        _sut.Start(_writer);
        _testScheduler.AdvanceBy(TimeSpan.FromMilliseconds(500).Ticks);
        
        // assert
        var calls = _writer.ReceivedCalls();
        _writer.Received().WriteLineAsync(Arg.Is<string>(x => x == JsonConvert.SerializeObject(expando)));
    }

    [Fact]
    public void Should_WriteEveryLine()
    {
        // arrange
        var dtString1 = _fixture.Create<DateTime>().ToString("yyyy-MM-ddTHH:mm:ss");
        var message1 = _fixture.Create<string>();

        var dtString2 = _fixture.Create<DateTime>().ToString("yyyy-MM-ddTHH:mm:ss");
        var message2 = _fixture.Create<string>();

        var string1 = $"{dtString1}: {message1}";
        var string2 = $"{dtString2}: {message2}";
        _fs.AddFile(_sut.LogFilePath, $"{string1}\n{string2}");
        _sut.Start(_writer);
        var firstString = JsonConvert.SerializeObject(GetExpando(dtString1, message1));
        var secondString = JsonConvert.SerializeObject(GetExpando(dtString2, message2));

        // act
        _testScheduler.AdvanceBy(TimeSpan.FromMilliseconds(500).Ticks);

        // assert
        _writer.Received().WriteLineAsync(Arg.Is<string>(x => x == firstString));
        _writer.Received().WriteLineAsync(Arg.Is<string>(x => x == secondString));
    }

    [Fact]
    public void Should_ClearFile_WhenLimitReached()
    {
        // Arrange
        _fs.AddFile(_sut.LogFilePath, new MockFileData(new byte[15010000]));
        _sut.Start(_writer);

        // Act
        _testScheduler.AdvanceBy(TimeSpan.FromMilliseconds(500).Ticks);

        // Assert
        _fs.File.Open(_sut.LogFilePath, FileMode.Open).Length.Should().Be(0);
    }

    private static IDictionary<string, object> GetExpando(string dtString, string message)
    {
        var dt = Strftime.Parse(dtString, _format, CultureInfo.InvariantCulture);
        var expando = new ExpandoObject() as IDictionary<string, object>;
        expando["@t"] = dt.ToString("o");
        expando["@mt"] = message;
        return expando;
    }
}
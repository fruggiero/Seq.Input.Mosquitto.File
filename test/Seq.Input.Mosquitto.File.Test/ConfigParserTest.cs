using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;

namespace Seq.Input.Mosquitto.File.Test;

public class ConfigParserTest
{
    private readonly MockFileSystem _fs = new();

    [Fact]
    public void Should_ParseConfig()
    {
        var path = Helper.LoadConfigFromResource(_fs, "mosquitto.conf");
        const string logPath = @"D:\mosquitto.log";
        var sut = new ConfigParser(_fs);

        var res = sut.Parse(path);

        res.Format.Should().Be("%Y-%m-%dT%H:%M:%S");
        res.LogFilePath.Should().Be(logPath);
    }
}
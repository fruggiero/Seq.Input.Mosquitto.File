using NSubstitute;

namespace Seq.Input.Mosquitto.File.Test;

public class MosquittoInputTest
{
    [Fact]
    public void Should_Start()
    {
        // arrange
        var sut = new MosquittoInput();
        var writer = Substitute.For<TextWriter>();
        
        // act
        sut.Start(writer);

        // assert
        writer.Received().WriteLine("");
    }
}
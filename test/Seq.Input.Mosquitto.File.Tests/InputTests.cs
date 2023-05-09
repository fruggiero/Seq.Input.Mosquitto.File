using System.Collections.ObjectModel;

namespace Seq.Input.Mosquitto.File.Tests
{
	public class InputTests
	{
		[Fact]
		public void Should()
		{
			var sut = new MosquittoInput();
			var app = new Seq.Apps.App("Mosquitto", "Mosquitto", new ReadOnlyDictionary<string, string>(), "");
			
			sut = new 
			sut.Start();
		}
	}
}
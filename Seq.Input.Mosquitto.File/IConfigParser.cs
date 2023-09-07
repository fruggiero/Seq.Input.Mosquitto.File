namespace Seq.Input.Mosquitto.File
{
    internal interface IConfigParser
    {
        LogConfig Parse(string filepath);
    }
}
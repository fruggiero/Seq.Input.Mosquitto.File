using System;

namespace Seq.Input.Mosquitto.File
{
    public class InvalidConfigException : Exception
    {
        public InvalidConfigException(string message) : 
            base($"Error during Mosquitto configuration parsing: {message}")
        {
        }
    }
}
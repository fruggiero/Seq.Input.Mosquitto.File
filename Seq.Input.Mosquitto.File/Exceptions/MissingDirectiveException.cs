using System;

namespace Seq.Input.Mosquitto.File.Exceptions
{
    public class MissingDirectiveException : Exception
    {
        public MissingDirectiveException(string message) : base(message)
        {
            
        }
    }
}
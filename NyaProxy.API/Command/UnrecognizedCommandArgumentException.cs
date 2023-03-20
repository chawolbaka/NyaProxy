﻿using System;

namespace NyaProxy.API.Command
{
    public class UnrecognizedArgumentException : CommandException
    {
        public Command Command { get; set; }
        public string Argument { get; set; } 

        public UnrecognizedArgumentException(Command command, string argument) : base(command.Name)
        {
            Command = command;
            Argument = argument;
        }
        
        public UnrecognizedArgumentException(Command command, string argument, string message) : base(command.Name, message)
        {
            Command = command;
            Argument = argument;
        }

        public UnrecognizedArgumentException(Command command, string argument, string message, Exception innerException) : base(command.Name, message, innerException)
        {
            Command = command;
            Argument = argument;
        }
    }
}

﻿namespace Bhp.Plugins
{
    public interface ILogPlugin
    {
        void Log(string source, LogLevel level, string message);
    }
}

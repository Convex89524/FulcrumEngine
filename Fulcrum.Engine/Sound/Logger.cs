// Copyright (C) 2025-2029 Convex89524
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3 (GPLv3 only).
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using CMLS.CLogger;

namespace Fulcrum.Engine.Sound
{
    public interface ILogger
    {
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
        void Debug(string msg);
    }

    public sealed class ConsoleLogger : ILogger
    {
        public static Clogger LOGGER = LogManager.GetLogger("SoundEngine");
        
        private readonly string _tag;
        public ConsoleLogger(string tag = "Sound") => _tag = tag;

        public void Info(string msg)  => Write(LogLevel.INFO,  msg);
        public void Warn(string msg)  => Write(LogLevel.WARN,  msg);
        public void Error(string msg) => Write(LogLevel.ERROR, msg);
        public void Debug(string msg) => Write(LogLevel.DEBUG, msg);

        private void Write(LogLevel level, string msg)
        {
            if (level == LogLevel.ERROR) LOGGER.Error(msg);
            if (level == LogLevel.WARN) LOGGER.Warn(msg);
            if (level == LogLevel.INFO) LOGGER.Info(msg);
            if (level == LogLevel.DEBUG) LOGGER.Debug(msg);
        }
    }
}
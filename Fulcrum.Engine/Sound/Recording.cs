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
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Fulcrum.Engine.Sound
{
    public sealed class Recorder : IDisposable
    {
        private readonly ILogger _log;
        private WasapiCapture? _cap;
        private WaveFileWriter? _writer;

        public Recorder(ILogger log) { _log = log; }

        public void StartToWav(string outputPath, bool loopback = false)
        {
            if (_cap != null) throw new InvalidOperationException("Recorder already running.");

            _cap = loopback ? new WasapiLoopbackCapture() : new WasapiCapture();
            _writer = new WaveFileWriter(outputPath, _cap.WaveFormat);

            _cap.DataAvailable += (_, e) =>
            {
                _writer!.Write(e.Buffer, 0, e.BytesRecorded);
            };
            _cap.RecordingStopped += (_, e) =>
            {
                _writer?.Dispose(); _writer = null;
                _cap?.Dispose(); _cap = null;
                if (e.Exception != null) _log.Error($"Recording stopped with error: {e.Exception}");
                else _log.Info("Recording stopped.");
            };
            _cap.StartRecording();
            _log.Info($"Recording started -> {outputPath}");
        }

        public void Stop()
        {
            _cap?.StopRecording();
        }

        public void Dispose()
        {
            Stop();
            _writer?.Dispose();
            _cap?.Dispose();
        }
    }
}
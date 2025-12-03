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

#define HAS_NAUDIO_VORBIS

using System;
using System.IO;
using System.Runtime.InteropServices;
using Fulcrum.Common;
using NAudio.Wave;
using NAudio.MediaFoundation;
using NAudio.Wave.SampleProviders;

#if HAS_NAUDIO_VORBIS
using NAudio.Vorbis;
#endif

namespace Fulcrum.Engine.Sound
{
    public interface IAudioDecoder : IDisposable
    {
        ISampleProvider ToSampleProvider();
        WaveFormat Format { get; }
        TimeSpan TotalTime { get; }
        void Seek(TimeSpan pos);
    }

    internal static class DecoderUtil
    {
        public static ISampleProvider ToFloatProvider(WaveStream ws)
        {
            return ws.ToSampleProvider();
        }

        public static string NormalizeExt(string path)
            => Path.GetExtension(path).ToLowerInvariant();
    }

    public sealed class WavDecoder : IAudioDecoder
    {
        private readonly WaveFileReader _r;
        public WavDecoder(string path) { _r = new WaveFileReader(path); }
        public ISampleProvider ToSampleProvider() => DecoderUtil.ToFloatProvider(_r);
        public WaveFormat Format => _r.WaveFormat;
        public TimeSpan TotalTime => _r.TotalTime;
        public void Seek(TimeSpan pos) => _r.CurrentTime = pos;
        public void Dispose() => _r.Dispose();
    }

    public sealed class Mp3Decoder : IAudioDecoder
    {
        private readonly WaveStream _ws;
        private readonly bool _mf;
        public Mp3Decoder(string path)
        {
            try {
                _ws = new Mp3FileReader(path);
                _mf = false;
            } catch {
                _ws = new MediaFoundationReader(path);
                _mf = true;
            }
        }
        public ISampleProvider ToSampleProvider() => DecoderUtil.ToFloatProvider(_ws);
        public WaveFormat Format => _ws.WaveFormat;
        public TimeSpan TotalTime => (_ws as AudioFileReader)?.TotalTime ?? (_ws as MediaFoundationReader)?.TotalTime ?? TimeSpan.Zero;
        public void Seek(TimeSpan pos)
        {
            if (_mf && _ws is MediaFoundationReader mfr) mfr.CurrentTime = pos;
            else _ws.CurrentTime = pos;
        }
        public void Dispose() => _ws.Dispose();
    }

    public sealed class MfContainerDecoder : IAudioDecoder
    {
        private readonly MediaFoundationReader _r;
        public MfContainerDecoder(string path)
        {
            _r = new MediaFoundationReader(path);
        }
        public ISampleProvider ToSampleProvider() => _r.ToSampleProvider();
        public WaveFormat Format => _r.WaveFormat;
        public TimeSpan TotalTime => _r.TotalTime;
        public void Seek(TimeSpan pos) => _r.CurrentTime = pos;
        public void Dispose() => _r.Dispose();
    }

    /// <summary>
    /// OGG/Vorbis
    /// </summary>
    /// <summary>
    /// OGG/Vorbis 解码器
    /// </summary>
    public sealed class OggVorbisDecoder : IAudioDecoder
    {
        #if HAS_NAUDIO_VORBIS
        private readonly VorbisWaveReader _r;
        
        public OggVorbisDecoder(string path) 
        { 
            _r = new VorbisWaveReader(path); 
        }
        
        public ISampleProvider ToSampleProvider() => DecoderUtil.ToFloatProvider(_r);
        public WaveFormat Format => _r.WaveFormat;
        public TimeSpan TotalTime => _r.TotalTime;
        
        public void Seek(TimeSpan pos) 
        { 
            _r.CurrentTime = pos; 
        }
        
        public void Dispose() 
        { 
            _r?.Dispose(); 
        }
        #else
        public OggVorbisDecoder(string path)
        {
            throw new NotSupportedException(GetErrorMessage());
        }
    
        public ISampleProvider ToSampleProvider() => throw new NotSupportedException(GetErrorMessage());
        public WaveFormat Format => WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public TimeSpan TotalTime => TimeSpan.Zero;
        public void Seek(TimeSpan pos) => throw new NotSupportedException(GetErrorMessage());
        public void Dispose() { }
    
        private static string GetErrorMessage()
        {
            return "OGG/Vorbis 解码功能未启用。\n" +
                   "请安装 NuGet 包 `NAudio.Vorbis`，并在项目文件中定义常量 `HAS_NAUDIO_VORBIS`。\n" +
                   "安装命令: Install-Package NAudio.Vorbis\n" +
                   "或考虑转换为 .wav/.mp3 等受支持格式。";
        }
    #endif
    }

    public sealed class AudioFileFallbackDecoder : IAudioDecoder
    {
        private readonly AudioFileReader _r;
        public AudioFileFallbackDecoder(string path) { _r = new AudioFileReader(path); }
        public ISampleProvider ToSampleProvider() => _r.ToSampleProvider();
        public WaveFormat Format => _r.WaveFormat;
        public TimeSpan TotalTime => _r.TotalTime;
        public void Seek(TimeSpan pos) => _r.CurrentTime = pos;
        public void Dispose() => _r.Dispose();
    }

    public static class DecoderFactory
    {
        public static IAudioDecoder FromFile(string path)
        {
            path = Path.Combine(Global.ResourcesPath, "sounds", path);
            
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("音频路径为空。");
            if (!File.Exists(path))
                throw new FileNotFoundException($"音频文件不存在：{path}");

            var ext = DecoderUtil.NormalizeExt(path);

            try
            {
                return ext switch
                {
                    ".wav" => new WavDecoder(path),
                    ".mp3" => new Mp3Decoder(path),
                    ".ogg" => new OggVorbisDecoder(path),
                    ".m4a" or ".aac" or ".wma" or ".mp4" or ".asf" => new MfContainerDecoder(path),
                    _ => new AudioFileFallbackDecoder(path),
                };
            }
            catch (COMException comEx) when ((uint)comEx.ErrorCode == 0xC00D36C4)
            {
                throw new NotSupportedException(
                    $"该文件由系统 Media Foundation 报‘不支持的字节流类型’：{path}\n" +
                    $"扩展名：{ext}\n" +
                    $"可能原因：1) 这是 OGG/Vorbis（请安装 NAudio.Vorbis）；" +
                    $"2) 这是 m4a/aac/wma，但系统缺少 Media Feature Pack；" +
                    $"3) 文件损坏或非常规编码。\n" +
                    $"建议：若为 .ogg，请安装 NuGet: NAudio.Vorbis 并定义 HAS_NAUDIO_VORBIS；" +
                    $"若为 m4a/aac，请确认系统已安装媒体功能组件；或转换为 .wav/.mp3。", comEx);
            }
        }
    }
}

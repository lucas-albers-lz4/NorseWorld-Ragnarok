/*
 *  "NorseWorld: Ragnarok", a roguelike game for PCs.
 *  Copyright (C) 2002-2008, 2014 by Serg V. Zhdanovskih.
 *
 *  This file is part of "NorseWorld: Ragnarok".
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using NVorbis;
using SDL2;
using ZRLib.Core;

namespace NWR.Game
{
    internal static class OggChunkLoader
    {
        public const int TargetFrequency = 44100;
        public const int TargetChannels = 2;

        private static readonly Dictionary<string, IntPtr> fCache = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<IntPtr, GCHandle> fPinnedPcm = new Dictionary<IntPtr, GCHandle>();

        public static IntPtr LoadChunk(string path, bool cache)
        {
            if (string.IsNullOrEmpty(path)) {
                return IntPtr.Zero;
            }

            string key = Path.GetFullPath(path);
            IntPtr cached;
            if (cache && fCache.TryGetValue(key, out cached)) {
                return cached;
            }

            IntPtr chunk = DecodeToChunk(path);
            if (chunk != IntPtr.Zero && cache) {
                fCache[key] = chunk;
            }
            return chunk;
        }

        public static void FreeAll()
        {
            foreach (IntPtr chunk in fCache.Values) {
                if (chunk != IntPtr.Zero) {
                    SDL_mixer.Mix_FreeChunk(chunk);
                }
            }
            fCache.Clear();

            foreach (GCHandle handle in fPinnedPcm.Values) {
                if (handle.IsAllocated) {
                    handle.Free();
                }
            }
            fPinnedPcm.Clear();
        }

        private static IntPtr DecodeToChunk(string path)
        {
            try {
                byte[] pcm = DecodeOggToPcm(path);
                if (pcm == null || pcm.Length == 0) {
                    return IntPtr.Zero;
                }

                GCHandle handle = GCHandle.Alloc(pcm, GCHandleType.Pinned);
                IntPtr chunk = SDL_mixer.Mix_QuickLoad_RAW(pcm, (uint)pcm.Length);
                if (chunk == IntPtr.Zero) {
                    handle.Free();
                    return IntPtr.Zero;
                }

                fPinnedPcm[chunk] = handle;
                return chunk;
            } catch (Exception ex) {
                Logger.Write("OggChunkLoader.DecodeToChunk(): " + ex.Message);
                return IntPtr.Zero;
            }
        }

        private static byte[] DecodeOggToPcm(string path)
        {
            using (var reader = new VorbisReader(path)) {
                int channels = reader.Channels;
                int sampleRate = reader.SampleRate;
                var samples = new List<float>(sampleRate * channels);

                float[] buffer = new float[4096 * channels];
                int read;
                while ((read = reader.ReadSamples(buffer, 0, buffer.Length)) > 0) {
                    for (int i = 0; i < read; i++) {
                        samples.Add(buffer[i]);
                    }
                }

                if (samples.Count == 0) {
                    return null;
                }

                int frameCount = samples.Count / channels;
                short[] frames = new short[frameCount * channels];
                for (int i = 0; i < samples.Count; i++) {
                    float sample = samples[i];
                    if (sample > 1.0f) {
                        sample = 1.0f;
                    } else if (sample < -1.0f) {
                        sample = -1.0f;
                    }
                    frames[i] = (short)(sample * 32767.0f);
                }

                short[] stereo = ToStereo(frames, channels);
                short[] resampled = Resample(stereo, sampleRate, TargetFrequency, TargetChannels);
                return ShortsToBytes(resampled);
            }
        }

        private static short[] ToStereo(short[] frames, int channels)
        {
            if (channels == TargetChannels) {
                return frames;
            }

            int frameCount = frames.Length / channels;
            short[] stereo = new short[frameCount * TargetChannels];
            if (channels == 1) {
                for (int i = 0; i < frameCount; i++) {
                    short sample = frames[i];
                    stereo[i * 2] = sample;
                    stereo[i * 2 + 1] = sample;
                }
            } else {
                for (int i = 0; i < frameCount; i++) {
                    stereo[i * 2] = frames[i * channels];
                    stereo[i * 2 + 1] = frames[i * channels + 1];
                }
            }
            return stereo;
        }

        private static short[] Resample(short[] frames, int srcRate, int dstRate, int channels)
        {
            if (srcRate == dstRate) {
                return frames;
            }

            int srcFrameCount = frames.Length / channels;
            int dstFrameCount = (int)((long)srcFrameCount * dstRate / srcRate);
            if (dstFrameCount <= 0) {
                return frames;
            }

            short[] result = new short[dstFrameCount * channels];
            for (int dstFrame = 0; dstFrame < dstFrameCount; dstFrame++) {
                double srcPos = (double)dstFrame * srcRate / dstRate;
                int srcIndex = (int)srcPos;
                double frac = srcPos - srcIndex;
                int nextIndex = srcIndex + 1;
                if (nextIndex >= srcFrameCount) {
                    nextIndex = srcFrameCount - 1;
                }

                for (int ch = 0; ch < channels; ch++) {
                    short s0 = frames[srcIndex * channels + ch];
                    short s1 = frames[nextIndex * channels + ch];
                    result[dstFrame * channels + ch] = (short)(s0 + frac * (s1 - s0));
                }
            }
            return result;
        }

        private static byte[] ShortsToBytes(short[] samples)
        {
            byte[] bytes = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}

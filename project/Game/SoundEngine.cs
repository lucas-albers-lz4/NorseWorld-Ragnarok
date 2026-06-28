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
using System.IO;
using BSLib;
using SDL2;
using ZRLib.Core;

namespace NWR.Game
{
    public sealed class SoundEngine
    {
        public const int sk_Sound = 0;
        public const int sk_Song = 1;
        public const int sk_Ambient = 2;

        private const int AmbientChannel = 0;
        private const int MaxSounds = 64;
        private const int MaxDistance = 80;
        private const int MixVolumeMax = 128;

        public enum Reverb
        {
            Room,
            Cave,
            Arena,
            Forest,
            Mountains,
            Underwater,
            Dungeon,
            CHall,
            Quarry,
            Plain
        }

        private sealed class SoundData
        {
            public int Kind;
            public int Channel;
            public IntPtr Chunk;
        }

        private readonly SoundData[] fSndList;
        private readonly int[] fSndVolume;

        private IntPtr fCurrentMusic;
        private int fSndCount;
        private bool fSndReady;
        private float fReverbScale;

        public SoundEngine()
        {
            fSndVolume = new int[3];
            fSndList = new SoundData[MaxSounds];
            for (int i = 0; i < MaxSounds; i++) {
                fSndList[i] = new SoundData();
            }
            fReverbScale = 1.0f;
        }

        public void SfxSetReverb(Reverb reverb)
        {
            if (GlobalVars.Debug_Silent || !fSndReady) {
                return;
            }

            switch (reverb) {
                case Reverb.Underwater:
                    fReverbScale = 0.75f;
                    break;
                case Reverb.Cave:
                case Reverb.Dungeon:
                    fReverbScale = 0.9f;
                    break;
                case Reverb.Mountains:
                case Reverb.Plain:
                    fReverbScale = 1.0f;
                    break;
                default:
                    fReverbScale = 0.95f;
                    break;
            }
        }

        public void SfxDone()
        {
            try {
                if (GlobalVars.Debug_Silent || !fSndReady) {
                    return;
                }

                StopAllPlayback();
                OggChunkLoader.FreeAll();

                if (fCurrentMusic != IntPtr.Zero) {
                    SDL_mixer.Mix_FreeMusic(fCurrentMusic);
                    fCurrentMusic = IntPtr.Zero;
                }

                SDL_mixer.Mix_CloseAudio();
                SDL_mixer.Mix_Quit();
                fSndReady = false;
            } catch (Exception ex) {
                Logger.Write("SoundEngine.sfxDone(): " + ex.Message);
            }
        }

        public void SfxInit(int wnd)
        {
            try {
                if (GlobalVars.Debug_Silent) {
                    return;
                }

                if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_AUDIO) != 0) {
                    throw new Exception("SDL_InitSubSystem(AUDIO): " + SDL.SDL_GetError());
                }

                int oggFlag = (int)SDL_mixer.MIX_InitFlags.MIX_INIT_OGG;
                int initted = SDL_mixer.Mix_Init((SDL_mixer.MIX_InitFlags)oggFlag);
                if ((initted & oggFlag) != oggFlag) {
                    throw new Exception("Mix_Init(OGG): " + SDL.SDL_GetError());
                }

                if (SDL_mixer.Mix_OpenAudio(OggChunkLoader.TargetFrequency, SDL_mixer.MIX_DEFAULT_FORMAT, OggChunkLoader.TargetChannels, 4096) != 0) {
                    throw new Exception("Mix_OpenAudio(): " + SDL.SDL_GetError());
                }

                SDL_mixer.Mix_AllocateChannels(MaxSounds);
                SDL_mixer.Mix_ReserveChannels(1);

                fSndReady = true;
                fSndCount = 0;
                fCurrentMusic = IntPtr.Zero;

                for (int i = 0; i < MaxSounds; i++) {
                    fSndList[i].Kind = -1;
                    fSndList[i].Channel = -1;
                    fSndList[i].Chunk = IntPtr.Zero;
                }

                fSndVolume[0] = 255;
                fSndVolume[1] = 255;
                fSndVolume[2] = 255;
                fReverbScale = 1.0f;
            } catch (Exception ex) {
                Logger.Write("SoundEngine.sfxInit(): " + ex.Message);
            }
        }

        public void SfxPlay(string fileName, int kind)
        {
            SfxPlay(fileName, kind, ExtPoint.Empty, ExtPoint.Empty);
        }

        public void SfxPlay(string fileName, int kind, ExtPoint player, ExtPoint sound)
        {
            if (GlobalVars.Debug_Silent) {
                return;
            }

            if (!fSndReady) {
                Logger.Write("SFX kernel not initialized");
                return;
            }

            if (fSndVolume[kind] == 0) {
                return;
            }

            string path = Path.Combine(
                NWResourceManager.GetAppPath(),
                fileName.Replace('\\', Path.DirectorySeparatorChar));

            if (!File.Exists(path)) {
                Logger.Write(string.Format("Media file \"{0}\" not exists", new object[] { path }));
                return;
            }

            if (kind == sk_Song) {
                PlaySong(path);
                return;
            }

            if (kind == sk_Ambient) {
                PlayAmbient(path);
                return;
            }

            PlaySoundEffect(path, kind, player, sound);
        }

        public void SfxSetVolume(int volume, int aKind)
        {
            if (GlobalVars.Debug_Silent) {
                return;
            }

            if (!fSndReady) {
                Logger.Write("SFX kernel not initialized");
                return;
            }

            fSndVolume[aKind] = volume;
            int mixVolume = ToMixVolume(volume);

            if (aKind == sk_Song) {
                SDL_mixer.Mix_VolumeMusic(mixVolume);
                return;
            }

            if (aKind == sk_Ambient) {
                SDL_mixer.Mix_Volume(AmbientChannel, mixVolume);
            }

            for (int i = 0; i < fSndCount; i++) {
                if (fSndList[i].Kind == aKind && fSndList[i].Channel >= 0) {
                    SDL_mixer.Mix_Volume(fSndList[i].Channel, mixVolume);
                }
            }
        }

        public void SfxResume()
        {
            if (GlobalVars.Debug_Silent) {
                return;
            }

            if (!fSndReady) {
                Logger.Write("SFX kernel not initialized");
                return;
            }

            SDL_mixer.Mix_Resume(-1);
            SDL_mixer.Mix_ResumeMusic();
        }

        public void SfxSuspend()
        {
            if (GlobalVars.Debug_Silent) {
                return;
            }

            if (!fSndReady) {
                Logger.Write("SFX kernel not initialized");
                return;
            }

            SDL_mixer.Mix_Pause(-1);
            SDL_mixer.Mix_PauseMusic();
        }

        private void PlaySong(string path)
        {
            SDL_mixer.Mix_HaltMusic();

            if (fCurrentMusic != IntPtr.Zero) {
                SDL_mixer.Mix_FreeMusic(fCurrentMusic);
                fCurrentMusic = IntPtr.Zero;
            }

            fCurrentMusic = SDL_mixer.Mix_LoadMUS(path);
            if (fCurrentMusic == IntPtr.Zero) {
                Logger.Write("Mix_LoadMUS(): " + SDL.SDL_GetError());
                return;
            }

            if (SDL_mixer.Mix_PlayMusic(fCurrentMusic, -1) != 0) {
                Logger.Write("Mix_PlayMusic(): " + SDL.SDL_GetError());
                return;
            }

            SDL_mixer.Mix_VolumeMusic(ToMixVolume(fSndVolume[sk_Song]));
            TrackSlot(sk_Song, -1, IntPtr.Zero);
        }

        private void PlayAmbient(string path)
        {
            SDL_mixer.Mix_HaltChannel(AmbientChannel);
            // Ambient loops disabled (Tier C placeholders mask combat SFX during testing).
        }

        private void PlaySoundEffect(string path, int kind, ExtPoint player, ExtPoint sound)
        {
            int slot = FindReplaceSlot(kind);
            if (slot < 0) {
                if (fSndCount >= MaxSounds) {
                    slot = 0;
                    if (fSndList[slot].Channel >= 0) {
                        SDL_mixer.Mix_HaltChannel(fSndList[slot].Channel);
                    }
                } else {
                    slot = fSndCount;
                    fSndCount++;
                }
            } else if (fSndList[slot].Channel >= 0) {
                SDL_mixer.Mix_HaltChannel(fSndList[slot].Channel);
            }

            IntPtr chunk = OggChunkLoader.LoadChunk(path, true);
            if (chunk == IntPtr.Zero) {
                Logger.Write("OggChunkLoader.LoadChunk(): failed for " + path);
                return;
            }

            int channel = SDL_mixer.Mix_PlayChannel(-1, chunk, 0);
            if (channel < 0) {
                Logger.Write("Mix_PlayChannel(): " + SDL.SDL_GetError());
                return;
            }

            int volume = ToMixVolume(fSndVolume[kind]);
            volume = ApplyDistanceVolume(volume, player, sound);
            volume = ApplyReverbVolume(volume);
            SDL_mixer.Mix_Volume(channel, volume);

            fSndList[slot].Kind = kind;
            fSndList[slot].Channel = channel;
            fSndList[slot].Chunk = chunk;
        }

        private int FindReplaceSlot(int kind)
        {
            for (int idx = 0; idx < fSndCount; idx++) {
                if (fSndList[idx].Kind != kind) {
                    continue;
                }

                if (kind == sk_Song || kind == sk_Ambient) {
                    return idx;
                }

                int channel = fSndList[idx].Channel;
                if (channel < 0 || SDL_mixer.Mix_Playing(channel) == 0) {
                    return idx;
                }
            }
            return -1;
        }

        private void TrackSlot(int kind, int channel, IntPtr chunk)
        {
            int slot = FindReplaceSlot(kind);
            if (slot < 0) {
                slot = fSndCount;
                fSndCount++;
            }

            fSndList[slot].Kind = kind;
            fSndList[slot].Channel = channel;
            fSndList[slot].Chunk = chunk;
        }

        private static int ApplyDistanceVolume(int volume, ExtPoint player, ExtPoint sound)
        {
            if (sound.X < 0 || sound.Y < 0) {
                return volume;
            }

            int dx = player.X - sound.X;
            int dy = player.Y - sound.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double scale = Math.Max(0.0, 1.0 - dist / MaxDistance);
            return (int)(volume * scale);
        }

        private int ApplyReverbVolume(int volume)
        {
            return (int)(volume * fReverbScale);
        }

        private static int ToMixVolume(int volume255)
        {
            return (volume255 * MixVolumeMax) / 255;
        }

        private void StopAllPlayback()
        {
            SDL_mixer.Mix_HaltMusic();
            SDL_mixer.Mix_HaltChannel(-1);

            for (int i = 0; i < fSndCount; i++) {
                fSndList[i].Kind = -1;
                fSndList[i].Channel = -1;
                fSndList[i].Chunk = IntPtr.Zero;
            }
            fSndCount = 0;
        }
    }
}

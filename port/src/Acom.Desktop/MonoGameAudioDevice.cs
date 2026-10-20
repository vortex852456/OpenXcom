/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This file is part of ACOM, a C# port of OpenXcom Extended.
 *
 * ACOM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ACOM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Acom.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Audio;

namespace Acom.Desktop;

/// <summary>
/// Audio output of the engine's <see cref="Mixer"/> through a MonoGame <see cref="DynamicSoundEffectInstance"/>,
/// which pulls the mixed signal in blocks of the opened chunk size.
/// </summary>
/// <remarks>
/// MonoGame raises <see cref="DynamicSoundEffectInstance.BufferNeeded"/> from its game loop, so the mixing
/// (and the music hooks) run on the main thread between frames; a few queued blocks cover frame-time jitter.
/// Without audio hardware (headless, no OpenAL device) <see cref="Open"/> fails and the game runs muted.
/// </remarks>
internal sealed class MonoGameAudioDevice : IAudioDevice, IDisposable
{
	/// <summary>Blocks kept queued (MonoGame's own target for dynamic instances).</summary>
	private const int QueuedBuffers = 3;

	private DynamicSoundEffectInstance? _instance;
	private short[] _mix = [];

	public bool Open(int frequency, int channels, int samples)
	{
		Close();
		try
		{
			var instance = new DynamicSoundEffectInstance(frequency, channels == 1 ? AudioChannels.Mono : AudioChannels.Stereo);
			_mix = new short[samples * channels];
			_instance = instance;
			instance.BufferNeeded += OnBufferNeeded;
			while (instance.PendingBufferCount < QueuedBuffers)
				Submit(instance);
			instance.Play();
			return true;
		}
#pragma warning disable CA1031 // any failure (no OpenAL device, unsupported rate) means "no audio output", like SDL_OpenAudio failing
		catch (Exception e)
#pragma warning restore CA1031
		{
			Logger.Warning($"Audio output: {e.Message}");
			Close();
			return false;
		}
	}

	public void Close()
	{
		var instance = _instance;
		_instance = null;
		if (instance != null)
		{
			instance.BufferNeeded -= OnBufferNeeded;
			instance.Stop();
			instance.Dispose();
		}
	}

	public void Dispose() => Close();

	private void OnBufferNeeded(object? sender, EventArgs e)
	{
		var instance = _instance;
		if (instance == null)
			return;
		while (instance.PendingBufferCount < QueuedBuffers)
			Submit(instance);
	}

	private void Submit(DynamicSoundEffectInstance instance)
	{
		Mixer.Mix(_mix);
		// a new array per block: the platform may keep the submitted buffer until it has been played
		var bytes = new byte[_mix.Length * sizeof(short)];
		MemoryMarshal.AsBytes(_mix.AsSpan()).CopyTo(bytes); // 16-bit little endian (all supported hosts)
		instance.SubmitBuffer(bytes);
	}
}

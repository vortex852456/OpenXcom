// Golden reference for Engine/Mixer.cs: the SDL_mixer 1.2 calls the game uses, run against the real
// library with SDL's dummy audio driver; a post-mix hook records the mixed blocks.
// Build/run: port/tools/golden/generate.sh (needs the SDL_mixer 1.2 dev package).
// Output (mixer.txt), compared by MixerTests.cs, which plays the same scenarios:
//   <scenario> <hex int16 samples of one block>   per mixed block
//   state <scenario> <Mix_Playing(-1)> <Mix_PlayingMusic()>   after the blocks
//   channel/position/music lines: return values of the calls
// Fades are left out: SDL_mixer times them with SDL_GetTicks.
#include <SDL.h>
#include <SDL_mixer.h>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <vector>

static const int RATE = 11025, CHANNELS = 2, SAMPLES = 64;
static FILE *f;
static std::vector<std::vector<Sint16> > blocks;
static volatile int wanted = 0;
static int statePlaying, stateMusic;

static void postmix(void *, Uint8 *stream, int len)
{
	if (wanted > 0)
	{
		blocks.push_back(std::vector<Sint16>((Sint16 *)stream, (Sint16 *)(stream + len)));
		if (wanted == 1)
		{
			// state right after the last block; then no more mixing until the next scenario
			statePlaying = Mix_Playing(-1);
			stateMusic = Mix_PlayingMusic();
			SDL_PauseAudio(1);
		}
		--wanted;
	}
}

// Captures the next n blocks; the scenario was set up while the audio was locked.
static void capture(const char *name, int n)
{
	blocks.clear();
	wanted = n;
	SDL_PauseAudio(0);
	SDL_UnlockAudio();
	while (wanted > 0)
		SDL_Delay(1);
	SDL_LockAudio();
	for (auto &b : blocks)
	{
		fprintf(f, "%s ", name);
		for (Sint16 s : b) fprintf(f, "%04x", (unsigned)(Uint16)s);
		fprintf(f, "\n");
	}
	fprintf(f, "state %s %d %d\n", name, statePlaying, stateMusic);
}

static std::vector<Uint8> wav(int rate, int channels, int bits, const std::vector<Uint8> &data)
{
	std::vector<Uint8> w;
	auto u32 = [&](Uint32 v) { for (int i = 0; i < 4; ++i) w.push_back((v >> (8 * i)) & 0xff); };
	auto u16 = [&](Uint16 v) { w.push_back(v & 0xff); w.push_back(v >> 8); };
	auto tag = [&](const char *t) { for (int i = 0; i < 4; ++i) w.push_back(t[i]); };
	tag("RIFF"); u32(36 + data.size()); tag("WAVE");
	tag("fmt "); u32(16); u16(1); u16(channels); u32(rate); u32(rate * channels * bits / 8); u16(channels * bits / 8); u16(bits);
	tag("data"); u32(data.size());
	w.insert(w.end(), data.begin(), data.end());
	return w;
}

// Test sounds, built the same way in MixerTests.cs
static std::vector<Uint8> mono8(int n)
{
	std::vector<Uint8> d;
	for (int i = 0; i < n; ++i) d.push_back((Uint8)(128 + ((i * 37) % 200) - 100));
	return wav(RATE, 1, 8, d);
}

static std::vector<Uint8> stereo16(int n)
{
	std::vector<Uint8> d;
	for (int i = 0; i < n; ++i)
	{
		Sint16 l = (Sint16)(i * 300 - 9000), r = (Sint16)(20000 - i * 450);
		d.push_back(l & 0xff); d.push_back((l >> 8) & 0xff);
		d.push_back(r & 0xff); d.push_back((r >> 8) & 0xff);
	}
	return wav(RATE, 2, 16, d);
}

static Mix_Chunk *load(const std::vector<Uint8> &w)
{
	Mix_Chunk *c = Mix_LoadWAV_RW(SDL_RWFromConstMem(w.data(), w.size()), 1);
	if (!c) { fprintf(stderr, "%s\n", Mix_GetError()); exit(1); }
	return c;
}

static void hook(void *udata, Uint8 *stream, int len)
{
	int *phase = (int *)udata;
	Sint16 *s = (Sint16 *)stream;
	for (int i = 0; i < len / 2; ++i, ++*phase) s[i] = (Sint16)((*phase % 50) * 200 - 5000);
}

int main(int argc, char **argv)
{
	if (argc != 2) { fprintf(stderr, "usage: mixer <outfile>\n"); return 1; }
	setenv("SDL_AUDIODRIVER", "dummy", 1);
	if (SDL_Init(SDL_INIT_AUDIO) < 0 || Mix_OpenAudio(RATE, AUDIO_S16LSB, CHANNELS, SAMPLES) != 0)
	{
		fprintf(stderr, "%s\n", SDL_GetError());
		return 1;
	}
	f = fopen(argv[1], "w");
	SDL_LockAudio();
	Mix_SetPostMix(postmix, 0);
	// Game::initAudio
	Mix_AllocateChannels(16);
	Mix_ReserveChannels(5);
	Mix_GroupChannels(1, 2, 0);
	Mix_Chunk *a = load(mono8(150)), *b = load(stereo16(100)), *c = load(mono8(40));

	// channels: group allocation, free channel search, volumes, looping, clipping, stereo position
	// channel <played on> <Mix_GroupAvailable afterwards>
	int played = Mix_PlayChannel(Mix_GroupAvailable(0), a, 0);
	fprintf(f, "channel %d %d\n", played, Mix_GroupAvailable(0));
	played = Mix_PlayChannel(-1, b, 0);
	fprintf(f, "channel %d %d\n", played, Mix_GroupAvailable(0));
	played = Mix_PlayChannel(3, c, 2);
	fprintf(f, "channel %d %d\n", played, Mix_GroupAvailable(-1));
	Mix_Volume(-1, 100);
	Mix_Volume(1, 64);
	Mix_Volume(3, 50);
	fprintf(f, "position %d\n", Mix_SetPosition(5, 280, 100));
	capture("mix", 6);

	// stereo position on asymmetric input, angle by angle
	const int angles[] = { 0, 45, 80, 90, 135, 180, 200, 270, 300, 359 };
	for (int angle : angles)
	{
		Mix_PlayChannel(6, b, 0);
		fprintf(f, "position %d\n", Mix_SetPosition(6, angle, angle / 3));
		capture("angle", 1);
		Mix_HaltChannel(6);
	}

	// music hook (FlcPlayer, AdlibMusic) before the channels; Mix_VolumeMusic does not touch it
	int phase = 0;
	Mix_HookMusic(hook, &phase);
	Mix_VolumeMusic(10);
	Mix_PlayChannel(-1, c, 0);
	capture("hook", 2);
	Mix_HookMusic(NULL, NULL);

	// WAV music: plays twice at the music volume, then stops
	std::vector<Uint8> m = stereo16(90);
	Mix_Music *music = Mix_LoadMUS_RW(SDL_RWFromConstMem(m.data(), m.size()));
	if (!music) { fprintf(stderr, "%s\n", Mix_GetError()); return 1; }
	Mix_VolumeMusic(96);
	int rc = Mix_PlayMusic(music, 2);
	fprintf(f, "music %d %d\n", rc, Mix_GetMusicType(0));
	capture("music", 4);
	Mix_HaltMusic();

	// halting
	Mix_PlayChannel(-1, a, -1);
	Mix_PlayChannel(-1, b, -1);
	Mix_HaltChannel(-1);
	capture("halt", 1);

	SDL_UnlockAudio();
	Mix_CloseAudio();
	SDL_Quit();
	fclose(f);
	return 0;
}

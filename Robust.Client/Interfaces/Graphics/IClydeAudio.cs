using System.IO;
using Robust.Client.Audio;

namespace Robust.Client.Interfaces.Graphics
{
    public interface IClydeAudio
    {
        // AUDIO SYSTEM DOWN BELOW.
        AudioStream LoadAudioOggVorbis(Stream stream, string name = null);
        AudioStream LoadAudioWav(Stream stream, string name = null);

        IClydeAudioSource CreateAudioSource(AudioStream stream);
        IClydeBufferedAudioSource CreateBufferedAudioSource(int buffers);
    }
}
/*
 * Originally from the Unity Community Wiki
 * http://wiki.unity3d.com/index.php/Saving_AudioClip_as_WAV
 * * This is a public domain utility class for saving AudioClips as WAV files.
 */

using System;
using System.IO;
using UnityEngine;

public class WavUtility
{
    private const int HEADER_SIZE = 44;

    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            // --- WAV Header ---
            // RIFF chunk
            stream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
            stream.Write(BitConverter.GetBytes(HEADER_SIZE + clip.samples * 2), 0, 4); // File size - 8
            stream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);

            // "fmt " sub-chunk (format)
            stream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
            stream.Write(BitConverter.GetBytes(16), 0, 4); // Sub-chunk size (16 for PCM)
            stream.Write(BitConverter.GetBytes((ushort)1), 0, 2); // Audio format (1 = PCM)
            stream.Write(BitConverter.GetBytes((ushort)clip.channels), 0, 2);
            stream.Write(BitConverter.GetBytes(clip.frequency), 0, 4);
            stream.Write(BitConverter.GetBytes(clip.frequency * clip.channels * 2), 0, 4); // Byte rate
            stream.Write(BitConverter.GetBytes((ushort)(clip.channels * 2)), 0, 2); // Block align
            stream.Write(BitConverter.GetBytes((ushort)16), 0, 2); // Bits per sample

            // "data" sub-chunk
            stream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
            stream.Write(BitConverter.GetBytes(clip.samples * clip.channels * 2), 0, 4); // Data size

            // --- Audio Data ---
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Convert float samples (-1.0 to 1.0) to 16-bit PCM (short)
            foreach (float sample in samples)
            {
                short pcmSample = (short)(sample * 32767.0f);
                stream.Write(BitConverter.GetBytes(pcmSample), 0, 2);
            }

            return stream.ToArray();
        }
    }
    public static AudioClip ToAudioClip(byte[] wavFile, string clipName = "GeminiClip")
    {
        WAV wav = new WAV(wavFile);
        AudioClip audioClip = AudioClip.Create(clipName, wav.SampleCount, 1, wav.Frequency, false);
        audioClip.SetData(wav.LeftChannel, 0);
        return audioClip;
    }
    // Helper class for reading WAV byte data
    public class WAV
    {
        public float[] LeftChannel { get; private set; }
        public int ChannelCount { get; private set; }
        public int SampleCount { get; private set; }
        public int Frequency { get; private set; }

        public WAV(byte[] wav)
        {
            // Read channel count
            ChannelCount = wav[22]; // 1 = mono, 2 = stereo
            Frequency = BitConverter.ToInt32(wav, 24);

            // Find where the actual data chunk begins
            int pos = 12;
            while (!(wav[pos] == 'd' && wav[pos + 1] == 'a' && wav[pos + 2] == 't' && wav[pos + 3] == 'a'))
            {
                pos += 4;
                int chunkSize = BitConverter.ToInt32(wav, pos);
                pos += 4 + chunkSize;
            }
            pos += 8;

            SampleCount = (wav.Length - pos) / 2; // 16-bit audio
            LeftChannel = new float[SampleCount];

            // Convert 16-bit PCM data to float samples (-1.0 to 1.0)
            int i = 0;
            while (pos < wav.Length)
            {
                short sample = BitConverter.ToInt16(wav, pos);
                LeftChannel[i] = sample / 32768.0f;
                pos += 2;
                i++;
            }
        }
    }


}
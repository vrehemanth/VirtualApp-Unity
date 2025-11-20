using UnityEngine;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

 
public class AudioRecorder : MonoBehaviour
{
    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool isRecording = false;
    private int startSample;
    private float startTime;

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log("Using Microphone: " + microphoneDevice);
        }
        else
        {
            Debug.LogError("No microphone detected!");
        }
    }

    public void StartRecording()
    {
        if (microphoneDevice == null) return;

        // Start recording with a large buffer (e.g., 300 sec)
        recordedClip = Microphone.Start(microphoneDevice, false, 300, 44100);
        isRecording = true;
        startTime = Time.time;
        MessageManager.Instance.ShowMessage("🎙️Recording started...");
        Debug.Log("🎙️Recording started...");
    }

    public void StopRecording()
    {
        MessageManager.Instance.ShowMessage("⏹️Recording stopped \n Processing...");
        Debug.Log("⏹️StopRecording() triggered");
        if (!isRecording) return;

        int endPosition = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        isRecording = false;

        // Calculate how many samples were recorded
        int samplesRecorded = endPosition;
        Debug.Log("Samples recorded: " + samplesRecorded);

        // Copy only the recorded part into a new trimmed clip
        float[] samples = new float[samplesRecorded * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create(
            "TrimmedClip",
            samplesRecorded,
            recordedClip.channels,
            recordedClip.frequency,
            false
        );
        trimmedClip.SetData(samples, 0);

        Debug.Log("Recording stopped. Duration: " + (Time.time - startTime) + "s");

        // Optional: save or send directly to LLM later
        // 🎯 Send audio to Gemini via GeminiLLM
        GeminiLLM.Instance.SendAudioToGemini(this, trimmedClip);
        // SaveWav("RecordedAudio.wav", trimmedClip);
        // Debug.Log("Saved trimmed audio to: " + Application.persistentDataPath);
    }

    //--- Helper: Save AudioClip as WAV ---
    void SaveWav(string filename, AudioClip clip)
    {
        var filepath = Path.Combine(Application.persistentDataPath, filename);
        byte[] wavData = WavUtility.FromAudioClip(clip);
        File.WriteAllBytes(filepath, wavData);
    }
}

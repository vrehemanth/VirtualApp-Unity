using UnityEngine;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class GeminiLLM : MonoBehaviour
{
    public static GeminiLLM Instance { get; private set; }
    private List<string> questions = new List<string>();
    private bool isPlayingQuestions = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // StartCoroutine(CheckAvailableModels());
            // GenerateAndPlayAudioFromText(this, "Welcome to the virtual classroom!");
            StartCoroutine(GenerateAudioOnly("Welcome to the virtual classroom!", (clip) =>
            {
                AudioSource src = GetComponent<AudioSource>();
                if (!src) src = gameObject.AddComponent<AudioSource>();
                MessageManager.Instance.ShowMessage("Welcome to the virtual classroom!");
                src.clip = clip;
                src.Play();
            }));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called externally to send an AudioClip to Gemini.
    /// </summary>
    public void SendAudioToGemini(MonoBehaviour caller, AudioClip clip)
    {
        caller.StartCoroutine(SendAudioToGeminiCoroutine(clip));
    }

    private IEnumerator SendAudioToGeminiCoroutine(AudioClip clip)
    {
        Debug.Log("Preparing audio for Gemini API...");

        // 1️⃣ Convert AudioClip to WAV bytes
        byte[] wavBytes = WavUtility.FromAudioClip(clip);

        // 2️⃣ Base64 encode
        string base64Audio = Convert.ToBase64String(wavBytes);

        // 3️⃣ Create JSON request
        string prompt = "This is an audio of a person speaking in the classroom. Please return an array of 2 or 3 questions in JSON format.";

        JObject requestJson = new JObject
        {
            ["contents"] = new JArray
            {
                new JObject
                {
                    ["parts"] = new JArray
                    {
                        new JObject { ["text"] = prompt },
                        new JObject
                        {
                            ["inline_data"] = new JObject
                            {
                                ["mime_type"] = "audio/wav",
                                ["data"] = base64Audio
                            }
                        }
                    }
                }
            }
        };

        string jsonData = requestJson.ToString();
        Debug.Log("Sending audio to Gemini..." + GeminiConfig.API_KEY);

        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" + GeminiConfig.API_KEY;
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Response from Gemini: " + request.downloadHandler.text);

            try
            {
                JObject response = JObject.Parse(request.downloadHandler.text);
                string textOutput = (string)response["candidates"]?[0]?["content"]?["parts"]?[0]?["text"];
                Debug.Log("🎯 Gemini Output:\n" + textOutput);

                string cleanedJson = textOutput
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
                JArray array = JArray.Parse(cleanedJson);

                questions.Clear();
                foreach (var item in array)
                {
                    string question = (string)item["question"];
                    questions.Add(question);
                }

                // Log the result as ["...","..."]
                Debug.Log("✅ Extracted Questions: \n" + string.Join("\n ", questions));
                // MessageManager.Instance.ShowMessage("Questions: \n " + string.Join("\n ", questions));

                // Start playing them
                StartCoroutine(PlayQuestionsSequentially());

            }
            catch (Exception ex)
            {
                Debug.LogError("Error parsing Gemini response: " + ex.Message);
            }
        }
        else
        {
            Debug.LogError("❌ Request failed: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }
    private IEnumerator PlayQuestionsSequentially()
    {
        if (isPlayingQuestions) yield break;
        isPlayingQuestions = true;

        foreach (var q in questions)
        {
            yield return StartCoroutine(GenerateAudioOnly(q, (clip) =>
            {
                StartCoroutine(HandleQuestionAudio(clip, q));  // Move logic here
            }));

            yield return new WaitForSeconds(GetComponent<AudioSource>().clip.length + 1f);
        }

        isPlayingQuestions = false;
    }
    private IEnumerator HandleQuestionAudio(AudioClip clip, string q)
    {
        // Student stands
        StartCoroutine(StudentManager.Instance.StandForAudio(clip.length + 2.25f));

        // Wait before playing
        yield return new WaitForSeconds(2.25f);

        // Play audio
        AudioSource source = GetComponent<AudioSource>();
        if (!source) source = gameObject.AddComponent<AudioSource>();

        MessageManager.Instance.ShowMessage(q);
        source.clip = clip;
        source.Play();
    }

    private IEnumerator GenerateAudioOnly(string text, Action<AudioClip> callback)
    {
        yield return GenerateAndPlayAudioFromTextCoroutine(text, callback);
    }

    // public void GenerateAndPlayAudioFromText(MonoBehaviour caller, string inputText)
    // {
    //     caller.StartCoroutine(GenerateAndPlayAudioFromTextCoroutine(inputText));
    // }

    private IEnumerator GenerateAndPlayAudioFromTextCoroutine(string inputText, Action<AudioClip> callback = null)
    {
        Debug.Log("🗣️ Sending text to Gemini for audio generation...");

        // 1️⃣ Prepare request JSON - CORRECTED
        JObject requestJson = new JObject
        {
            ["contents"] = new JArray
            {
                new JObject
                {
                    ["parts"] = new JArray
                    {
                        new JObject
                        {
                            ["text"] = inputText // Remove the extra instruction - just use the text directly
                        }
                    }
                }
            },
            ["generationConfig"] = new JObject
            {
                ["responseModalities"] = new JArray { "AUDIO" }, // CORRECT: Use responseModalities instead of response_mime_type
                ["speechConfig"] = new JObject
                {
                    ["voiceConfig"] = new JObject
                    {
                        ["prebuiltVoiceConfig"] = new JObject
                        {
                            ["voiceName"] = "Puck" // or "Puck", "Nova", etc.
                        }
                    }
                }
            },
            // ADD THIS: Specify audio output format
            // ["audioConfig"] = new JObject
            // {
            //     ["audioEncoding"] = "LINEAR16", // This should give you WAV format
            //     ["speakingRate"] = 1.0,
            //     ["pitch"] = 0.0
            //     // You can also add: "sampleRateHertz": 24000 if needed
            // }
        };

        string jsonData = requestJson.ToString();
        
        // ✅ CORRECTED: Use the actual TTS model
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-tts:generateContent?key=" + GeminiConfig.API_KEY;

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Audio response received from Gemini.");
            // Debug.Log("Raw response: " + request.downloadHandler.text); // For debugging

            try
            {
                JObject response = JObject.Parse(request.downloadHandler.text);
                
                // CORRECTED: Fixed the path to audio data
                string base64Audio = (string)response["candidates"]?[0]?["content"]?["parts"]?[0]?["inlineData"]?["data"];
                Debug.Log("Response MIME: " + (string)response["candidates"]?[0]?["content"]?["parts"]?[0]?["inlineData"]?["mimeType"]);

                if (string.IsNullOrEmpty(base64Audio))
                {
                    Debug.LogError("❌ No audio data found in Gemini response.");
                    Debug.LogError("Full response: " + response.ToString());
                    MessageManager.Instance.ShowMessage("❌ Gemini did not return audio.");
                    yield break;
                }
                int sampleRate = 24000; // from the MIME type
                short bitsPerSample = 16;
                short channels = 1; // Gemini outputs mono

                byte[] pcm = Convert.FromBase64String(base64Audio);
                byte[] wavData = AddWavHeader(pcm, sampleRate, bitsPerSample, channels);
                // // 2️⃣ Decode base64 to bytes
                // byte[] audioBytes = Convert.FromBase64String(base64Audio);

                // 3️⃣ Convert to AudioClip and play
                AudioClip clip = WavUtility.ToAudioClip(wavData, "Gemini_TTS");
                if (clip != null)
                {
                    AudioSource audioSource = GetComponent<AudioSource>();
                    if (audioSource == null)
                        audioSource = gameObject.AddComponent<AudioSource>();

                    if (callback != null)
                    {
                        callback(clip);
                    }
                    else
                    {
                        audioSource.clip = clip;
                        audioSource.Play();
                    }


                    // MessageManager.Instance.ShowMessage("🎧 Playing generated speech...");
                    Debug.Log("🎧 Playing Gemini-generated speech. Duration: " + clip.length + " seconds");
                }
                else
                {
                    Debug.LogError("❌ Failed to convert audio bytes to AudioClip.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("❌ Error parsing Gemini audio response: " + ex.Message);
                Debug.LogError("Stack trace: " + ex.StackTrace);
                MessageManager.Instance.ShowMessage("❌ Error parsing Gemini audio response.");
            }
        }
        else
        {
            Debug.LogError("❌ Gemini TTS request failed: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    private static byte[] AddWavHeader(byte[] pcmData, int sampleRate, short bitsPerSample, short channels)
    {
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = (short)(channels * bitsPerSample / 8);
        int dataLength = pcmData.Length;
        int chunkSize = 36 + dataLength;

        using (MemoryStream ms = new MemoryStream(44 + dataLength))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            // RIFF header
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(chunkSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt  subchunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // PCM chunk size
            bw.Write((short)1); // audio format = PCM
            bw.Write(channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)blockAlign);
            bw.Write(bitsPerSample);

            // data subchunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataLength);
            bw.Write(pcmData);

            return ms.ToArray();
        }
    }

    private IEnumerator CheckAvailableModels()
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models?key=" + GeminiConfig.API_KEY;
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Available models: " + request.downloadHandler.text);
            // Look for "gemini-2.5-flash-preview-tts" in the response
        }
        else
        {
            Debug.LogError("Failed to fetch models: " + request.error);
        }
    }
}

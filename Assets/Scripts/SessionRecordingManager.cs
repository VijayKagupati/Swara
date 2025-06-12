using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SessionRecordingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MicrophoneInputSystem micSystem;
    [SerializeField] private AudioSource recordingPlaybackSource;
    [SerializeField] private AudioMixerGroup outputMixerGroup;
    
    [Header("Recording Settings")]
    [SerializeField] private int recordingFrequency = 44100;
    [SerializeField] private int maxRecordingTime = 300; // In seconds (5 minutes)
    [SerializeField] private bool recordMicrophone = false;
    [SerializeField] private bool recordDrumEvents = true;
    
    [Header("Status")]
    [SerializeField] private bool isRecording = false;
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private float currentRecordingTime = 0f;
    
    // Private recording fields
    private AudioClip masterRecording;
    private List<DrumEvent> drumEvents = new List<DrumEvent>();
    private string sessionName;
    private DateTime sessionStartTime;
    
    // For multi-track export
    private Dictionary<string, List<AudioEvent>> instrumentTracks = new Dictionary<string, List<AudioEvent>>();
    
    [Serializable]
    public struct DrumEvent
    {
        public string drumId;
        public float timestamp;
        public float velocity;
        public AudioClip sampleUsed;
    }
    
    [Serializable]
    public struct AudioEvent
    {
        public float timestamp;
        public float duration;
        public AudioClip clip;
    }

    private void Start()
    {
        // Initialize if references not set
        if (recordingPlaybackSource == null)
        {
            GameObject playbackObj = new GameObject("RecordingPlayback");
            playbackObj.transform.parent = transform;
            recordingPlaybackSource = playbackObj.AddComponent<AudioSource>();
        }
        
        if (outputMixerGroup != null)
        {
            recordingPlaybackSource.outputAudioMixerGroup = outputMixerGroup;
        }
    }

    #region Recording Control
    
    public void ToggleRecording()
    {
        if (isRecording)
            StopRecording();
        else
            StartRecording();
    }
    
    public void TogglePlayback()
    {
        if (isPlaying)
            StopPlayback();
        else
            PlayRecording();
    }

    public void ToggleMic()
    {
        if (recordMicrophone)
        {
            recordMicrophone = false;
        }
        else
        {
            recordMicrophone = true;
        }
    }
    
    public void StartRecording()
    {
        if (isRecording) return;
        
        // Generate session name with timestamp
        sessionStartTime = DateTime.Now;
        sessionName = $"Session_{sessionStartTime:yyyyMMdd_HHmmss}";
        
        // Clear previous recording data
        currentRecordingTime = 0f;
        drumEvents.Clear();
        
        // Start microphone if needed
        if (recordMicrophone && micSystem != null)
        {
            micSystem.StartMicrophoneInput();
            
            // Create a new AudioClip for the master recording
            masterRecording = AudioClip.Create(sessionName, 
                                            recordingFrequency * maxRecordingTime, 
                                            1, recordingFrequency, false);
        }
        
        isRecording = true;
        Debug.Log($"Started recording session: {sessionName}");
        
        // Start recording timer
        StartCoroutine(RecordingTimer());
    }
    
    public void StopRecording()
    {
        if (!isRecording) return;
        
        isRecording = false;
        
        // Stop microphone if it was being used
        if (recordMicrophone && micSystem != null)
        {
            // Get final microphone data before stopping
            CaptureFinalMicrophoneData();
            micSystem.StopMicrophoneInput();
        }
        
        // Save recording
        SaveRecording();
        
        Debug.Log($"Stopped recording. Session length: {currentRecordingTime:F2} seconds");
    }
    
    private IEnumerator RecordingTimer()
    {
        while (isRecording)
        {
            currentRecordingTime += Time.deltaTime;
            
            // Check if we've hit max recording time
            if (currentRecordingTime >= maxRecordingTime)
            {
                Debug.Log("Reached maximum recording time");
                StopRecording();
                yield break;
            }
            
            // Sample audio from the microphone at regular intervals
            if (recordMicrophone && micSystem != null)
            {
                CaptureCurrentMicrophoneData();
            }
            
            yield return null;
        }
    }
    
    private void CaptureCurrentMicrophoneData()
    {
        // Implementation depends on how you want to sample the microphone
        // This would synchronize with the MicrophoneInputSystem
    }
    
    private void CaptureFinalMicrophoneData()
    {
        // Implementation to capture any final buffer data
    }
    #endregion
    
    #region Drum Events
    
    // Call this from your drum hit scripts
    public void RecordDrumHit(string drumId, float velocity, AudioClip sample)
    {
        if (!isRecording || !recordDrumEvents) return;
        
        DrumEvent drumEvent = new DrumEvent
        {
            drumId = drumId,
            timestamp = currentRecordingTime,
            velocity = velocity,
            sampleUsed = sample
        };
        
        drumEvents.Add(drumEvent);
    }
    #endregion
    
    #region Saving & Loading
    
    private void SaveRecording()
    {
        // Create directory if it doesn't exist
        string directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        Directory.CreateDirectory(directoryPath);
        
        // Save master audio file (if recorded)
        if (recordMicrophone && masterRecording != null)
        {
            string audioFilePath = Path.Combine(directoryPath, $"{sessionName}.wav");
            SavWav.Save(audioFilePath, masterRecording);
        }
        
        // Save drum events to JSON
        if (recordDrumEvents && drumEvents.Count > 0)
        {
            string drumEventsJson = JsonUtility.ToJson(new DrumEventList { events = drumEvents.ToArray() });
            string drumEventsPath = Path.Combine(directoryPath, $"{sessionName}_drums.json");
            File.WriteAllText(drumEventsPath, drumEventsJson);
        }
        
        Debug.Log($"Saved recording to {directoryPath}/{sessionName}");
    }
    
    [Serializable]
    private class DrumEventList
    {
        public DrumEvent[] events;
    }
    
    public void LoadRecording(string sessionId)
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        
        // Load master audio
        string audioFilePath = Path.Combine(directoryPath, $"{sessionId}.wav");
        if (File.Exists(audioFilePath))
        {
            StartCoroutine(LoadAudioClip(audioFilePath));
        }
        
        // Load drum events
        string drumEventsPath = Path.Combine(directoryPath, $"{sessionId}_drums.json");
        if (File.Exists(drumEventsPath))
        {
            string json = File.ReadAllText(drumEventsPath);
            DrumEventList loadedEvents = JsonUtility.FromJson<DrumEventList>(json);
            drumEvents = new List<DrumEvent>(loadedEvents.events);
        }
    }
    
    private IEnumerator LoadAudioClip(string filePath)
    {
        using (WWW www = new WWW("file://" + filePath))
        {
            yield return www;
            masterRecording = www.GetAudioClip();
            recordingPlaybackSource.clip = masterRecording;
        }
    }
    #endregion
    
    #region Playback
    
    public void PlayRecording()
    {
        if (isPlaying || masterRecording == null) return;
        
        isPlaying = true;
        recordingPlaybackSource.clip = masterRecording;
        recordingPlaybackSource.Play();
        
        // Start drum event playback coroutine
        if (drumEvents.Count > 0)
        {
            StartCoroutine(PlaybackDrumEvents());
        }
    }
    
    public void StopPlayback()
    {
        if (!isPlaying) return;
        
        isPlaying = false;
        recordingPlaybackSource.Stop();
        StopAllCoroutines();
    }
    
    private IEnumerator PlaybackDrumEvents()
    {
        float startTime = Time.time;
        int eventIndex = 0;
        
        while (isPlaying && eventIndex < drumEvents.Count)
        {
            float playbackTime = Time.time - startTime;
            
            if (playbackTime >= drumEvents[eventIndex].timestamp)
            {
                PlayDrumEvent(drumEvents[eventIndex]);
                eventIndex++;
            }
            
            yield return null;
        }
    }
    
    private void PlayDrumEvent(DrumEvent drumEvent)
    {
        // This would trigger the playback of the drum sample
        // Could use object pooling for efficient playback
        GameObject tempAudio = new GameObject($"DrumPlayback_{drumEvent.drumId}");
        tempAudio.transform.parent = transform;
        
        AudioSource source = tempAudio.AddComponent<AudioSource>();
        source.clip = drumEvent.sampleUsed;
        source.volume = drumEvent.velocity;
        source.Play();
        
        // Clean up after playing
        StartCoroutine(CleanupAudioSource(source));
    }
    
    private IEnumerator CleanupAudioSource(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        Destroy(source.gameObject);
    }
    #endregion
    
    #region Session Management
    
    public string[] GetAllSavedSessions()
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        if (!Directory.Exists(directoryPath)) return new string[0];
        
        // Get all WAV files
        string[] wavFiles = Directory.GetFiles(directoryPath, "*.wav");
        
        // Extract session names
        string[] sessionNames = new string[wavFiles.Length];
        for (int i = 0; i < wavFiles.Length; i++)
        {
            sessionNames[i] = Path.GetFileNameWithoutExtension(wavFiles[i]);
        }
        
        return sessionNames;
    }
    
    public void DeleteSession(string sessionId)
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        
        // Delete audio file
        string audioFile = Path.Combine(directoryPath, $"{sessionId}.wav");
        if (File.Exists(audioFile)) File.Delete(audioFile);
        
        // Delete drum events
        string drumFile = Path.Combine(directoryPath, $"{sessionId}_drums.json");
        if (File.Exists(drumFile)) File.Delete(drumFile);
    }
    #endregion
}

// Helper class for saving AudioClips as WAV files
public static class SavWav
{
    public static bool Save(string path, AudioClip clip)
    {
        // Basic implementation
        if (!path.ToLower().EndsWith(".wav"))
        {
            path += ".wav";
        }
        
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);
        
        // Convert to 16-bit PCM
        byte[] buffer = ConvertToWav(samples, clip.channels, clip.frequency);
        
        try
        {
            using (FileStream fs = File.Create(path))
            {
                fs.Write(buffer, 0, buffer.Length);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save WAV file: {e.Message}");
            return false;
        }
    }
    
    private static byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        // Simple WAV header implementation
        using (MemoryStream stream = new MemoryStream())
        {
            int blockSize = 2 * channels;
            int dataSize = samples.Length * 2;
            int fileSize = 36 + dataSize;
            
            // RIFF header
            WriteString(stream, "RIFF");
            WriteInt(stream, fileSize);
            WriteString(stream, "WAVE");
            
            // Format chunk
            WriteString(stream, "fmt ");
            WriteInt(stream, 16);
            WriteShort(stream, 1); // PCM format
            WriteShort(stream, (short)channels);
            WriteInt(stream, sampleRate);
            WriteInt(stream, sampleRate * blockSize);
            WriteShort(stream, (short)blockSize);
            WriteShort(stream, 16); // Bits per sample
            
            // Data chunk
            WriteString(stream, "data");
            WriteInt(stream, dataSize);
            
            // Convert samples to 16-bit
            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)(samples[i] * 32767f);
                WriteShort(stream, value);
            }
            
            return stream.ToArray();
        }
    }
    
    private static void WriteString(MemoryStream stream, string str)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(str);
        stream.Write(bytes, 0, bytes.Length);
    }
    
    private static void WriteInt(MemoryStream stream, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        stream.Write(bytes, 0, 4);
    }
    
    private static void WriteShort(MemoryStream stream, short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        stream.Write(bytes, 0, 2);
    }
}
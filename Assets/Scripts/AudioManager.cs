using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System;
using System.IO;

public class AudioManager : MonoBehaviour
{
    #region Singleton Pattern
    public static AudioManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAudio();
    }
    #endregion

    [Header("Audio Mixer")]
    public AudioMixer mainMixer;
    public AudioMixerGroup masterGroup;
    public AudioMixerGroup vocalsGroup;
    public AudioMixerGroup drumsGroup;
    public AudioMixerGroup synthsGroup;
    public AudioMixerGroup ambientGroup;

    [Header("Microphone Settings")]
    public bool isMicrophoneActive = false;
    public string selectedMicrophone;
    public int microphoneSampleRate = 44100;
    private AudioClip microphoneClip;
    private AudioSource microphoneSource;

    [Header("Recording")]
    public bool isRecording = false;
    private float recordingStartTime;
    private List<AudioEvent> recordedEvents = new List<AudioEvent>();
    
    [Serializable]
    public struct AudioEvent
    {
        public enum EventType { DrumHit, SynthToggle, VocalSample }
        
        public EventType type;
        public float timestamp;
        public string soundId;
        public bool isStarting; // For toggles, true = starting, false = stopping
        public Vector3 position;
    }

    private void InitializeAudio()
    {
        // Initialize microphone source
        microphoneSource = gameObject.AddComponent<AudioSource>();
        microphoneSource.outputAudioMixerGroup = vocalsGroup;
        microphoneSource.spatialBlend = 1.0f; // Full 3D
        microphoneSource.loop = true;
        
        // Load available microphones
        if (Microphone.devices.Length > 0)
        {
            selectedMicrophone = Microphone.devices[0];
            Debug.Log($"Available microphones: {string.Join(", ", Microphone.devices)}");
        }
        else
        {
            Debug.LogWarning("No microphone detected!");
        }
    }

    #region Microphone Methods
    public void StartMicrophone()
    {
        if (string.IsNullOrEmpty(selectedMicrophone)) return;
        
        if (isMicrophoneActive)
        {
            StopMicrophone();
        }
        
        microphoneClip = Microphone.Start(selectedMicrophone, true, 10, microphoneSampleRate);
        microphoneSource.clip = microphoneClip;
        
        // Wait until the microphone starts recording
        while (!(Microphone.GetPosition(selectedMicrophone) > 0)) { }
        
        microphoneSource.Play();
        isMicrophoneActive = true;
        Debug.Log($"Started microphone: {selectedMicrophone}");
    }
    
    public void StopMicrophone()
    {
        if (!isMicrophoneActive) return;
        
        Microphone.End(selectedMicrophone);
        microphoneSource.Stop();
        isMicrophoneActive = false;
        Debug.Log("Stopped microphone");
    }
    #endregion

    #region Recording Methods
    public void StartRecording()
    {
        recordedEvents.Clear();
        recordingStartTime = Time.time;
        isRecording = true;
        Debug.Log("Recording started");
    }
    
    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"Recording stopped. Recorded {recordedEvents.Count} events.");
    }
    
    public void RecordEvent(AudioEvent audioEvent)
    {
        if (!isRecording) return;
        
        audioEvent.timestamp = Time.time - recordingStartTime;
        recordedEvents.Add(audioEvent);
    }
    
    public void ExportRecording(string filename = "recording")
    {
        string path = Path.Combine(Application.persistentDataPath, $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        string json = JsonUtility.ToJson(new SerializableEventList { events = recordedEvents.ToArray() });
        File.WriteAllText(path, json);
        Debug.Log($"Recording exported to {path}");
    }
    
    [Serializable]
    private class SerializableEventList
    {
        public AudioEvent[] events;
    }
    #endregion

    #region Audio Mixing
    public void SetMixerVolume(string parameterName, float volumeNormalized)
    {
        // Convert normalized volume (0-1) to mixer range (typically -80dB to 0dB)
        float volumeValue = Mathf.Lerp(-80f, 0f, volumeNormalized);
        mainMixer.SetFloat(parameterName, volumeValue);
    }
    #endregion

    #region Utility Methods
    public AudioSource CreateSpatialAudioSource(GameObject parent, AudioMixerGroup mixerGroup)
    {
        AudioSource source = parent.AddComponent<AudioSource>();
        source.spatialBlend = 1.0f; // Full 3D
        source.rolloffMode = AudioRolloffMode.Custom;
        source.outputAudioMixerGroup = mixerGroup;
        
        // Configure spatial settings for concert hall environment
        source.dopplerLevel = 0.0f; // No doppler for musical instruments
        source.spread = 15f; // Some sound spread
        
        // Custom rolloff curve for concert hall
        AnimationCurve rolloff = new AnimationCurve();
        rolloff.AddKey(0.1f, 1.0f);
        rolloff.AddKey(5.0f, 0.8f);
        rolloff.AddKey(15.0f, 0.5f);
        rolloff.AddKey(30.0f, 0.0f);
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloff);
        
        return source;
    }
    #endregion
}
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SessionRecordingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MicrophoneInputSystem micSystem;
    [SerializeField] private AudioSource playbackSource;

    [Header("Recording Settings")]
    [SerializeField] private int recordingFrequency = 44100;
    [SerializeField] private int maxRecordingTime = 300; // In seconds
    [SerializeField] private bool recordMicrophone = true;

    [Header("Status")]
    [SerializeField] private bool isRecording = false;
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private float currentRecordingTime = 0f;

    // Recording data
    private float[] outputBuffer;
    private float[] microphoneBuffer;
    private List<float> recordedSamples = new List<float>();
    private int numSamples = 0;
    private int sampleRate;
    private string sessionName;

    private void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        outputBuffer = new float[1024];
        microphoneBuffer = new float[1024];
        
        if (playbackSource == null)
        {
            GameObject playbackObj = new GameObject("RecordingPlayback");
            playbackObj.transform.parent = transform;
            playbackSource = playbackObj.AddComponent<AudioSource>();
        }
    }

    public void ToggleRecording()
    {
        if (isRecording)
            StopRecording();
        else
            StartRecording();
    }

    public void ToggleMic()
    {
        if (recordMicrophone)
            recordMicrophone = false;
        else
            recordMicrophone = true;
    }
    public void TogglePlayback()
    {
        if (isPlaying)
            StopPlayback();
        else
            PlayRecording();
    }

    public void StartRecording()
    {
        if (isRecording) return;

        // Generate session name with timestamp
        sessionName = $"Session_{DateTime.Now:yyyyMMdd_HHmmss}";
        
        // Clear previous recording
        recordedSamples.Clear();
        numSamples = 0;
        currentRecordingTime = 0f;
        
        // Start microphone if enabled
        if (recordMicrophone && micSystem != null)
        {
            micSystem.StartMicrophoneInput();
            // Note: Even if monitoring is disabled, we still record the mic input
        }
        
        isRecording = true;
        
        // Start sampling audio
        StartCoroutine(RecordAudio());
        
        Debug.Log($"Started recording master output: {sessionName}");
    }
    
    public void StopRecording()
    {
        if (!isRecording) return;
        
        isRecording = false;
        
        // Stop microphone recording if needed
        if (recordMicrophone && micSystem != null)
        {
            micSystem.StopMicrophoneInput();
        }
        
        // Save the recorded audio
        SaveRecording();
        
        Debug.Log($"Stopped recording. Session length: {currentRecordingTime:F2} seconds");
    }
    
    private IEnumerator RecordAudio()
    {
        while (isRecording)
        {
            // Update recording time
            currentRecordingTime += Time.deltaTime;
            
            // Get current output data from AudioListener
            AudioListener.GetOutputData(outputBuffer, 0);
            
            // Get microphone data if enabled
            bool hasMicData = false;
            if (recordMicrophone && micSystem != null)
            {
                // Get microphone data even if monitoring is off
                AudioSource micSource = micSystem.GetMicrophoneSource();
                if (micSource != null && micSource.clip != null)
                {
                    int micPosition = Microphone.GetPosition(micSystem.GetSelectedMicrophone());
                    if (micPosition > 0 && micSource.clip.samples > 0)
                    {
                        micSource.clip.GetData(microphoneBuffer, micPosition % micSource.clip.samples);
                        hasMicData = true;
                    }
                }
            }
            
            // Mix output and microphone data
            for (int i = 0; i < outputBuffer.Length; i++)
            {
                float sample = outputBuffer[i];
                
                // Mix in microphone data if available
                if (hasMicData)
                {
                    sample = sample + microphoneBuffer[i];
                }
                
                // Add to recorded samples
                recordedSamples.Add(sample);
                numSamples++;
            }
            
            // Check if we've hit max recording time
            if (currentRecordingTime >= maxRecordingTime)
            {
                Debug.Log("Reached maximum recording time");
                StopRecording();
                yield break;
            }
            
            yield return null;
        }
    }
    
    private void SaveRecording()
    {
        // Create directory if it doesn't exist
        string directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        Directory.CreateDirectory(directoryPath);
        
        // Create AudioClip from recorded samples
        AudioClip recordedClip = AudioClip.Create(
            sessionName, 
            numSamples,
            1, // Mono
            sampleRate, 
            false
        );
        
        // Convert list to array and set AudioClip data
        float[] samples = recordedSamples.ToArray();
        recordedClip.SetData(samples, 0);
        
        // Save WAV file
        string filepath = Path.Combine(directoryPath, $"{sessionName}.wav");
        SavWav.Save(filepath, recordedClip);
        
        // Set as current clip for playback
        playbackSource.clip = recordedClip;
        
        Debug.Log($"Saved recording to {filepath}");
    }
    
    public void PlayRecording()
    {
        if (isPlaying || playbackSource.clip == null) return;
        
        isPlaying = true;
        playbackSource.Play();
    }
    
    public void StopPlayback()
    {
        if (!isPlaying) return;
        
        isPlaying = false;
        playbackSource.Stop();
    }
}
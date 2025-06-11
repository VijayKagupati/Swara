using UnityEngine;
using System.Collections;
using System.Linq;
using TMPro;

public class MicrophoneInputSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource microphoneSource;
    [SerializeField] private SpectrumVisualizer visualizer;
    
    [Header("Microphone Settings")]
    [SerializeField] private string[] availableMicrophones;
    [SerializeField] private string selectedMicrophone;
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int micBufferLength = 5; // In seconds
    
    [Header("Audio Processing")]
    [SerializeField] private float inputGain = 2.0f;
    [SerializeField] [Range(0f, 1f)] private float noiseSuppression = 0.05f;
    
    [Header("Monitoring Settings")]
    [SerializeField] private bool enableSelfMonitoring = true;
    [SerializeField] [Range(0f, 1f)] private float monitoringVolume = 0.5f;
    private float originalVolume;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI microphoneNameText;
    
    private bool isRecording = false;
    private float[] sampleBuffer;
    private int bufferSize = 1024;

    private void Start()
    {
        InitializeMicrophones();
        sampleBuffer = new float[bufferSize];
        
        // Store original volume setting
        if (microphoneSource != null)
            originalVolume = microphoneSource.volume;
    }
    
    public void CycleToNextMicrophone()
    {
        if (availableMicrophones == null || availableMicrophones.Length <= 1)
            return;
            
        int currentIndex = GetCurrentMicrophoneIndex();
        int nextIndex = (currentIndex + 1) % availableMicrophones.Length;
        
        SwitchMicrophone(nextIndex);
        UpdateMicrophoneNameDisplay();
    }
    
    public void UpdateMicrophoneNameDisplay()
    {
        if (microphoneNameText != null && !string.IsNullOrEmpty(selectedMicrophone))
        {
            microphoneNameText.text = $"Mic: {selectedMicrophone}";
        }
    }

    // Add this new method to toggle self-monitoring
    public void ToggleSelfMonitoring(bool enable)
    {
        if (microphoneSource == null) return;
        
        enableSelfMonitoring = enable;
        
        if (enableSelfMonitoring)
        {
            // Restore monitoring volume
            microphoneSource.volume = monitoringVolume;
        }
        else
        {
            // Mute output but keep processing audio
            microphoneSource.volume = 0f;
        }
    }
    
    private void InitializeMicrophones()
    {
        // Get all available microphones
        availableMicrophones = Microphone.devices;

        if (availableMicrophones.Length > 0)
        {
            selectedMicrophone = availableMicrophones[0];
            Debug.Log($"Found {availableMicrophones.Length} microphones. Selected: {selectedMicrophone}");
            UpdateMicrophoneNameDisplay();
        }
        else
        {
            Debug.LogWarning("No microphones detected!");
            if (microphoneNameText != null)
                microphoneNameText.text = "No microphones found";
        }
    }
    
    public void StartMicrophoneInput()
    {
        if (string.IsNullOrEmpty(selectedMicrophone) || isRecording)
            return;

        // Start the microphone and route it to the audio source
        microphoneSource.clip = Microphone.Start(selectedMicrophone, true, micBufferLength, sampleRate);
        microphoneSource.loop = true;

        // Set volume based on monitoring preference
        microphoneSource.volume = enableSelfMonitoring ? monitoringVolume : 0f;

        // Wait until microphone starts recording
        while (!(Microphone.GetPosition(selectedMicrophone) > 0)) { }

        microphoneSource.Play();
        isRecording = true;

        Debug.Log($"Started recording from microphone: {selectedMicrophone}");
    }
    
    public void StopMicrophoneInput()
    {
        if (!isRecording) return;
        
        Microphone.End(selectedMicrophone);
        microphoneSource.Stop();
        microphoneSource.clip = null;
        isRecording = false;
        
        Debug.Log("Stopped microphone recording");
    }
    
    private void Update()
    {
        if (isRecording && visualizer != null)
        {
            // Get audio spectrum data
            microphoneSource.GetSpectrumData(sampleBuffer, 0, FFTWindow.BlackmanHarris);
        
            // Debug logging to check if we're getting data
            // float sum = 0;
            // for (int i = 0; i < sampleBuffer.Length; i++) {
            //     sum += sampleBuffer[i];
            // }
            //
            // // Apply noise suppression and amplification
            // for (int i = 0; i < sampleBuffer.Length; i++) {
            //     if (sampleBuffer[i] < noiseSuppression) {
            //         sampleBuffer[i] = 0;
            //     } else {
            //         sampleBuffer[i] = sampleBuffer[i] * inputGain;
            //     }
            // }
            
            // Update visualizer with processed data
            visualizer.UpdateSpectrumData(sampleBuffer);
        }
    }
    public void SetInputGain(float gain)
    {
        inputGain = gain;
    }
    
    public void SwitchMicrophone(int index)
    {
        if (index < 0 || index >= availableMicrophones.Length)
            return;

        bool wasRecording = isRecording;

        if (wasRecording)
            StopMicrophoneInput();

        selectedMicrophone = availableMicrophones[index];
        
        // Update the UI
        UpdateMicrophoneNameDisplay();

        if (wasRecording)
            StartMicrophoneInput();
    }
    
    public string[] GetAvailableMicrophones()
    {
        return availableMicrophones;
    }
    
    public int GetCurrentMicrophoneIndex()
    {
        return System.Array.IndexOf(availableMicrophones, selectedMicrophone);
    }
    
    // Set monitoring volume level (when enabled)
    public void SetMonitoringVolume(float volume)
    {
        monitoringVolume = Mathf.Clamp01(volume);
        
        if (enableSelfMonitoring && microphoneSource != null)
            microphoneSource.volume = monitoringVolume;
    }
    
    public void SetNoiseSuppression(float volume)
    {
        noiseSuppression = volume;
    }
    
    // Toggle microphone on/off - perfect for UI buttons
    public void ToggleMicrophone()
    {
        if (isRecording)
            StopMicrophoneInput();
        else
            StartMicrophoneInput();
    }
}
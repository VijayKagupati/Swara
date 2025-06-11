using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System;
using System.IO;

public class AudioManager : MonoBehaviour
{
    #region Singleton & Core Components
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Mixer")]
    public AudioMixer mainMixer;
    public AudioMixerGroup masterGroup;
    public AudioMixerGroup vocalsGroup;
    public AudioMixerGroup drumsGroup;
    public AudioMixerGroup synthsGroup;
    public AudioMixerGroup ambientGroup;
    
    [Header("Room Acoustics")]
    public AudioReverbZone studioReverb;
    public AudioReverbZone concertHallReverb;
    
    // Rest of your fields remain the same
    #endregion
    
    // Setup code and other methods

    public void SetActiveAcousticEnvironment(string environment)
    {
        switch(environment.ToLower())
        {
            case "studio":
                studioReverb.enabled = true;
                concertHallReverb.enabled = false;
                break;
                
            case "concerthall":
            case "concert":
                studioReverb.enabled = false;
                concertHallReverb.enabled = true;
                break;
        }
    }
    
    // Add this method to configure spatial audio for instrument sources
    public AudioSource SetupInstrumentAudio(GameObject instrumentObject, AudioMixerGroup group, bool isStudio = false)
    {
        AudioSource source = instrumentObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = group;
        source.spatialBlend = 1.0f;  // Full 3D
        
        // Different settings based on environment
        if (isStudio) {
            // Studio has tighter, more controlled sound
            source.minDistance = 0.5f;
            source.maxDistance = 8f;
            source.rolloffMode = AudioRolloffMode.Custom;
            
            // Create tighter rolloff curve for studio
            AnimationCurve rolloff = new AnimationCurve();
            rolloff.AddKey(0f, 1.0f);
            rolloff.AddKey(1.0f, 0.8f);
            rolloff.AddKey(4.0f, 0.4f);
            rolloff.AddKey(8.0f, 0.0f);
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloff);
        } else {
            // Concert hall has more gradual falloff
            source.minDistance = 1f;
            source.maxDistance = 25f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
        }
        
        return source;
    }
}
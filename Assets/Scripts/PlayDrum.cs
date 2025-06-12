using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class PlayDrum : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private List<AudioClip> drumSamples = new List<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float maxPitchVariation = 0.1f;
    [SerializeField] [Range(0f, 10f)] private float velocityMultiplier = 1f;
    [SerializeField] [Range(0f, 1f)] private float minVolume = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float maxVolume = 1f;
    
    [SerializeField] private AudioSource audioSource;
    
    private void Awake()
    {
        if (!audioSource)
        { 
            audioSource = GetComponent<AudioSource>();
        }
        
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stick"))
        {
            if (drumSamples.Count == 0)
            {
                Debug.LogWarning("No drum samples assigned to PlayDrum component on " + gameObject.name);
                return;
            }

            // Calculate impact velocity from rigidbody (if present)
            float impactVelocity = 1f;
            if (other.attachedRigidbody != null)
            {
                impactVelocity = other.attachedRigidbody.velocity.magnitude;
            }

            // Select random sample
            AudioClip selectedSample = drumSamples[Random.Range(0, drumSamples.Count)];

            // Calculate random pitch variation
            float randomPitch = 1f + Random.Range(-maxPitchVariation, maxPitchVariation);
            audioSource.pitch = randomPitch;

            // Calculate volume based on velocity
            float volume = Mathf.Lerp(minVolume, maxVolume,
                Mathf.Clamp01(impactVelocity * velocityMultiplier / 10f));

            // Play the sample
            audioSource.PlayOneShot(selectedSample, volume); 
        }
    }
}
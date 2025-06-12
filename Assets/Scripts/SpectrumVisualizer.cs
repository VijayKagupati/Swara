using UnityEngine;

public class SpectrumVisualizer : MonoBehaviour
{
    [SerializeField] private Transform[] visualBars;
    [SerializeField] private float visualScaleFactor = 1000f;
    [SerializeField] private float smoothSpeed = 10f;
    
    private float[] targetScales;
    
    private void Start()
    {
        targetScales = new float[visualBars.Length];
    }
    
    public void UpdateSpectrumData(float[] spectrumData)
    {
        // Sample spectrum data to match number of visual bars
        int samplesPerBar = Mathf.FloorToInt(spectrumData.Length / visualBars.Length);
    
        for (int i = 0; i < visualBars.Length; i++)
        {
            float sum = 0;
            for (int j = 0; j < samplesPerBar; j++)
            {
                int index = i * samplesPerBar + j;
                if (index < spectrumData.Length)
                    sum += spectrumData[index];
            }
        
            // Get average value for this frequency range
            float average = sum / samplesPerBar;
            targetScales[i] = average * visualScaleFactor;
        
            // Debug the highest value to check scaling
            if (i == 0)
                Debug.Log($"First bar target scale: {targetScales[i]}");
        }
    }
    
    private void Update()
    {
        // Smooth the visual transition
        for (int i = 0; i < visualBars.Length; i++)
        {
            if (visualBars[i] != null)
            {
                Vector3 newScale = visualBars[i].localScale;
                newScale.x = Mathf.Lerp(newScale.x, targetScales[i], Time.deltaTime * smoothSpeed);
                newScale.x = Mathf.Max(0.05f, newScale.x); // Minimum scale
                visualBars[i].localScale = newScale;
            }
        }
    }
    
    // In SpectrumVisualizer.cs
    void OnGUI() {
        if (visualBars.Length > 0 && visualBars[0] != null) {
            GUI.Label(new Rect(10, 10, 300, 20), $"Bar 0 height: {visualBars[0].localScale.y:F3}");
        }
    }
}
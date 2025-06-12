using System;
using System.IO;
using UnityEngine;

public static class SavWav
{
    const int HEADER_SIZE = 44;

    public static bool Save(string filepath, AudioClip clip)
    {
        if (!filepath.ToLower().EndsWith(".wav"))
        {
            filepath += ".wav";
        }

        // Make sure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

        using (FileStream fs = CreateEmpty(filepath))
        {
            ConvertAndWrite(fs, clip);
            WriteHeader(fs, clip);
            return true;
        }
    }

    static FileStream CreateEmpty(string filepath)
    {
        FileStream fs = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < HEADER_SIZE; i++)
        {
            fs.WriteByte(emptyByte);
        }

        return fs;
    }

    static void ConvertAndWrite(FileStream fs, AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];

        // Convert float to Int16
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * 32767);
        }

        // Convert Int16 to byte[]
        byte[] bytesData = new byte[intData.Length * 2];
        for (int i = 0; i < intData.Length; i++)
        {
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            bytesData[i * 2] = byteArr[0];
            bytesData[i * 2 + 1] = byteArr[1];
        }

        // Write audio data to file
        fs.Write(bytesData, 0, bytesData.Length);
    }

    static void WriteHeader(FileStream fs, AudioClip clip)
    {
        // Go back to beginning of file
        fs.Seek(0, SeekOrigin.Begin);

        // RIFF header
        WriteString(fs, "RIFF");
        WriteInt(fs, (int)fs.Length - 8); // Chunk size
        WriteString(fs, "WAVE");

        // Format chunk
        WriteString(fs, "fmt ");
        WriteInt(fs, 16); // Subchunk1Size (16 for PCM)
        WriteShort(fs, 1); // Format = 1 (PCM)
        WriteShort(fs, (short)clip.channels); // Channels
        WriteInt(fs, clip.frequency); // Sample rate
        WriteInt(fs, clip.frequency * clip.channels * 2); // Byte rate
        WriteShort(fs, (short)(clip.channels * 2)); // Block align
        WriteShort(fs, 16); // Bits per sample

        // Data chunk
        WriteString(fs, "data");
        WriteInt(fs, (int)fs.Length - HEADER_SIZE);
    }

    static void WriteString(FileStream fs, string str)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(str);
        fs.Write(bytes, 0, bytes.Length);
    }

    static void WriteInt(FileStream fs, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        fs.Write(bytes, 0, 4);
    }

    static void WriteShort(FileStream fs, short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        fs.Write(bytes, 0, 2);
    }
}
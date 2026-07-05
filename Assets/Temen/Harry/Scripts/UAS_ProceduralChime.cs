using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UAS_ProceduralChime : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
    [SerializeField] private int sampleRate = 44100;

    private AudioSource audioSource;
    private AudioClip clip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    public void PlayChime()
    {
        EnsureClip();
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void EnsureClip()
    {
        if (clip != null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        const float duration = 1.8f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float value = 0f;
            for (int note = 0; note < notes.Length; note++)
            {
                float start = note * 0.22f;
                float local = t - start;
                if (local < 0f)
                {
                    continue;
                }

                float envelope = Mathf.Exp(-4.5f * local);
                value += Mathf.Sin(2f * Mathf.PI * notes[note] * local) * envelope;
                value += Mathf.Sin(2f * Mathf.PI * notes[note] * 2.01f * local) * envelope * 0.18f;
            }

            data[i] = Mathf.Clamp(value * 0.18f, -1f, 1f);
        }

        clip = AudioClip.Create("UAS_OriginalPicnicChime", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
    }
}

using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Audio
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambientSource;

        [Header("Volume Controls")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = Constants.MUSIC_BASE_VOLUME;
        [Range(0f, 1f)] public float sfxVolume = Constants.SFX_BASE_VOLUME;
        [Range(0f, 1f)] public float ambientVolume = Constants.AMBIENT_BASE_VOLUME;

        protected override void OnSingletonAwake()
        {
            UpdateVolumes();
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null || musicSource == null) return;
            
            if (musicSource.isPlaying && musicSource.clip == clip) return;

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }

        public void UpdateVolumes()
        {
            if (musicSource != null) musicSource.volume = musicVolume * masterVolume;
            if (sfxSource != null) sfxSource.volume = sfxVolume * masterVolume;
            if (ambientSource != null) ambientSource.volume = ambientVolume * masterVolume;
        }

        // Crossfade music đơn giản
        public void CrossfadeMusic(AudioClip newClip, float duration = Constants.AUDIO_CROSSFADE_DURATION)
        {
            StartCoroutine(CrossfadeRoutine(newClip, duration));
        }

        private System.Collections.IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
        {
            if (musicSource == null) yield break;

            float startVol = musicSource.volume;
            
            // Fade out
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in
            float targetVol = musicVolume * masterVolume;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, targetVol, t / duration);
                yield return null;
            }
            musicSource.volume = targetVol;
        }
    }
}

using UnityEngine;
using GanhHangRong.Core;

namespace GanhHangRong.Audio
{
    public class AudioManager : Singleton<AudioManager>
    {
        private const string MasterVolumePrefKey = "GHR_MasterVolume";
        private const string MusicVolumePrefKey = "GHR_MusicVolume";
        private const string SfxVolumePrefKey = "GHR_SfxVolume";
        private const string AmbientVolumePrefKey = "GHR_AmbientVolume";

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambientSource;

        [Header("Volume Controls")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = Constants.MUSIC_BASE_VOLUME;
        [Range(0f, 1f)] public float sfxVolume = Constants.SFX_BASE_VOLUME;
        [Range(0f, 1f)] public float ambientVolume = Constants.AMBIENT_BASE_VOLUME;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public float AmbientVolume => ambientVolume;

        protected override void OnSingletonAwake()
        {
            EnsureAudioSources();
            LoadVolumeSettings();
            UpdateVolumes();
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            EnsureAudioSources();
            if (clip == null || musicSource == null) return;
            if (musicSource.isPlaying && musicSource.clip == clip) return;

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void PlaySFX(AudioClip clip)
        {
            EnsureAudioSources();
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }

        public void UpdateVolumes()
        {
            EnsureAudioSources();
            if (musicSource != null) musicSource.volume = musicVolume * masterVolume;
            if (sfxSource != null) sfxSource.volume = sfxVolume * masterVolume;
            if (ambientSource != null) ambientSource.volume = ambientVolume * masterVolume;
        }

        public void SetMasterVolume(float value, bool save = true)
        {
            masterVolume = Mathf.Clamp01(value);
            UpdateVolumes();
            if (save) PlayerPrefs.SetFloat(MasterVolumePrefKey, masterVolume);
        }

        public void SetMusicVolume(float value, bool save = true)
        {
            musicVolume = Mathf.Clamp01(value);
            UpdateVolumes();
            if (save) PlayerPrefs.SetFloat(MusicVolumePrefKey, musicVolume);
        }

        public void SetSfxVolume(float value, bool save = true)
        {
            sfxVolume = Mathf.Clamp01(value);
            UpdateVolumes();
            if (save) PlayerPrefs.SetFloat(SfxVolumePrefKey, sfxVolume);
        }

        public void SetAmbientVolume(float value, bool save = true)
        {
            ambientVolume = Mathf.Clamp01(value);
            UpdateVolumes();
            if (save) PlayerPrefs.SetFloat(AmbientVolumePrefKey, ambientVolume);
        }

        public void CrossfadeMusic(AudioClip newClip, float duration = Constants.AUDIO_CROSSFADE_DURATION, bool loop = true)
        {
            EnsureAudioSources();
            if (newClip == null || musicSource == null) return;
            StartCoroutine(CrossfadeRoutine(newClip, duration, loop));
        }

        private System.Collections.IEnumerator CrossfadeRoutine(AudioClip newClip, float duration, bool loop)
        {
            if (musicSource == null) yield break;
            if (musicSource.isPlaying && musicSource.clip == newClip) yield break;

            float startVol = musicSource.volume;
            duration = Mathf.Max(0.01f, duration);

            if (musicSource.isPlaying)
            {
                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                    yield return null;
                }

                musicSource.Stop();
            }

            musicSource.clip = newClip;
            musicSource.loop = loop;
            musicSource.volume = 0f;
            musicSource.Play();

            float targetVol = musicVolume * masterVolume;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, targetVol, t / duration);
                yield return null;
            }

            musicSource.volume = targetVol;
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MasterVolumePrefKey, masterVolume);
            musicVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefKey, sfxVolume);
            ambientVolume = PlayerPrefs.GetFloat(AmbientVolumePrefKey, ambientVolume);
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null) musicSource = CreateSource("MusicSource", true);
            if (sfxSource == null) sfxSource = CreateSource("SfxSource", false);
            if (ambientSource == null) ambientSource = CreateSource("AmbientSource", true);
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            Transform existing = transform.Find(sourceName);
            AudioSource source = existing != null ? existing.GetComponent<AudioSource>() : null;

            if (source == null)
            {
                GameObject sourceObject = existing != null ? existing.gameObject : new GameObject(sourceName);
                sourceObject.transform.SetParent(transform, false);
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}

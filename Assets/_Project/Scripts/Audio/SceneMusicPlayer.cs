using UnityEngine;

namespace GanhHangRong.Audio
{
    public class SceneMusicPlayer : MonoBehaviour
    {
        [SerializeField] private string musicResourcePath;
        [SerializeField] private bool loop = true;
        [SerializeField] private float fadeDuration = 1.5f;

        private void Start()
        {
            PlayConfiguredMusic();
        }

        public void PlayConfiguredMusic()
        {
            if (string.IsNullOrWhiteSpace(musicResourcePath)) return;

            AudioClip clip = Resources.Load<AudioClip>(musicResourcePath);
            if (clip == null)
            {
                Debug.LogWarning("[SceneMusicPlayer] Missing music clip at Resources/" + musicResourcePath);
                return;
            }

            AudioManager manager = AudioManager.Instance;
            if (manager == null) return;

            manager.CrossfadeMusic(clip, fadeDuration, loop);
        }
    }
}

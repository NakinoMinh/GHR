using System;
using System.Collections;
using System.Collections.Generic;
using GanhHangRong.Core;
using GanhHangRong.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GanhHangRong.Audio
{
    public enum GameplaySfxCue
    {
        CupPickup,
        CupPlace,
        PourWater,
        AddIngredient,
        AddIce,
        StoveIgnite,
        KettleReady,
        WashCup,
        DrinkReady,
        ServeSuccess,
        Payment,
        CustomerArrive,
        OrderPlaced,
        Error,
        ShopOpen,
        ShopClose,
        MenuOpen,
        MenuClose
    }

    [DefaultExecutionOrder(-700)]
    [DisallowMultipleComponent]
    public class GameplaySfxManager : MonoBehaviour
    {
        private const string ResourceRoot = "GHR_SFX/";
        private const int VoiceCount = 6;
        private const float BoilingLoopVolume = 0.16f;

        private static GameplaySfxManager instance;

        private readonly Dictionary<GameplaySfxCue, AudioClip> clips = new Dictionary<GameplaySfxCue, AudioClip>();
        private readonly float[] lastPlayTimes = new float[Enum.GetValues(typeof(GameplaySfxCue)).Length];
        private readonly System.Random pitchRandom = new System.Random(8421);

        private AudioSource[] voices;
        private float[] voiceVolumeScales;
        private float[] voiceStartedTimes;
        private AudioSource boilingLoopSource;
        private AudioClip boilingLoopClip;
        private AudioManager audioManager;
        private float boilingFade;
        private float nextEventSubscriptionTime;

        public static GameplaySfxManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GameplaySfxManager>();
                }

                return instance;
            }
        }

        public int LoadedClipCount => clips.Count + (boilingLoopClip != null ? 1 : 0);
        public bool IsBoilingLoopPlaying => boilingLoopSource != null && boilingLoopSource.isPlaying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForGameplayScene()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TryCreate(SceneManager.GetActiveScene());
        }

        public static void Play(GameplaySfxCue cue)
        {
            GameplaySfxManager manager = Instance;
            if (manager == null)
            {
                TryCreate(SceneManager.GetActiveScene());
                manager = Instance;
            }

            if (manager != null)
            {
                manager.PlayCue(cue);
            }
        }

        public void Preview(GameplaySfxCue cue)
        {
            PlayCue(cue, true);
        }

        public int GetActiveVoiceCount()
        {
            if (voices == null) return 0;

            int count = 0;
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null && voices[i].isPlaying) count++;
            }
            return count;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCreate(scene);
        }

        private static void TryCreate(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene != SceneManager.GetActiveScene()) return;

            if (instance == null)
            {
                instance = FindAnyObjectByType<GameplaySfxManager>();
            }

            if (instance != null || FindAnyObjectByType<TeaCart>(FindObjectsInactive.Include) == null) return;

            GameObject host = new GameObject("GameplaySfxManager_Auto");
            host.AddComponent<GameplaySfxManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            for (int i = 0; i < lastPlayTimes.Length; i++) lastPlayTimes[i] = -100f;

            BuildAudioSources();
            LoadAudioClips();
            audioManager = AudioManager.Instance;
            SubscribeEvents();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void Start()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextEventSubscriptionTime)
            {
                nextEventSubscriptionTime = Time.unscaledTime + 5f;
                SubscribeEvents();
            }

            float outputGain = GetOutputGain();
            UpdateVoiceVolumes(outputGain);
            UpdateBoilingLoop(outputGain);
        }

        private void BuildAudioSources()
        {
            voices = new AudioSource[VoiceCount];
            voiceVolumeScales = new float[VoiceCount];
            voiceStartedTimes = new float[VoiceCount];

            for (int i = 0; i < VoiceCount; i++)
            {
                voices[i] = CreateSource($"SfxVoice_{i + 1:00}", false);
            }

            boilingLoopSource = CreateSource("BoilingLoop", true);
            boilingLoopSource.volume = 0f;
            boilingLoopSource.pitch = 0.98f;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = loop ? 160 : 128;
            return source;
        }

        private void LoadAudioClips()
        {
            clips.Clear();
            foreach (GameplaySfxCue cue in Enum.GetValues(typeof(GameplaySfxCue)))
            {
                CueSettings settings = GetSettings(cue);
                AudioClip clip = Resources.Load<AudioClip>(ResourceRoot + settings.ResourceName);
                if (clip != null)
                {
                    clips[cue] = clip;
                }
                else
                {
                    Debug.LogWarning($"[Gameplay SFX] Missing clip: {settings.ResourceName}");
                }
            }

            boilingLoopClip = Resources.Load<AudioClip>(ResourceRoot + "boiling_loop");
            if (boilingLoopClip != null)
            {
                boilingLoopSource.clip = boilingLoopClip;
            }
            else
            {
                Debug.LogWarning("[Gameplay SFX] Missing clip: boiling_loop");
            }
        }

        private void PlayCue(GameplaySfxCue cue, bool ignoreCooldown = false)
        {
            if (!clips.TryGetValue(cue, out AudioClip clip) || clip == null || voices == null) return;

            CueSettings settings = GetSettings(cue);
            int cueIndex = (int)cue;
            if (!ignoreCooldown && Time.unscaledTime < lastPlayTimes[cueIndex] + settings.Cooldown) return;
            lastPlayTimes[cueIndex] = Time.unscaledTime;

            int voiceIndex = FindAvailableVoice();
            AudioSource voice = voices[voiceIndex];
            if (voice.isPlaying) voice.Stop();

            float pitchT = (float)pitchRandom.NextDouble();
            voice.clip = clip;
            voice.pitch = Mathf.Lerp(settings.MinimumPitch, settings.MaximumPitch, pitchT);
            voiceVolumeScales[voiceIndex] = settings.Volume;
            voiceStartedTimes[voiceIndex] = Time.unscaledTime;
            voice.volume = settings.Volume * GetOutputGain();
            voice.Play();
        }

        private int FindAvailableVoice()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying) return i;
            }

            int oldestIndex = 0;
            float oldestTime = voiceStartedTimes[0];
            for (int i = 1; i < voiceStartedTimes.Length; i++)
            {
                if (voiceStartedTimes[i] < oldestTime)
                {
                    oldestIndex = i;
                    oldestTime = voiceStartedTimes[i];
                }
            }

            return oldestIndex;
        }

        private void UpdateVoiceVolumes(float outputGain)
        {
            if (voices == null) return;

            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null && voices[i].isPlaying)
                {
                    voices[i].volume = voiceVolumeScales[i] * outputGain;
                }
            }
        }

        private void UpdateBoilingLoop(float outputGain)
        {
            if (boilingLoopSource == null || boilingLoopClip == null) return;

            bool shouldPlay = CartItem.IsKettleHeating && Time.timeScale > 0.001f;
            float targetFade = shouldPlay ? 1f : 0f;
            boilingFade = Mathf.MoveTowards(boilingFade, targetFade, Time.unscaledDeltaTime * 3f);

            if (shouldPlay && !boilingLoopSource.isPlaying)
            {
                boilingLoopSource.clip = boilingLoopClip;
                boilingLoopSource.Play();
            }

            boilingLoopSource.volume = BoilingLoopVolume * boilingFade * outputGain;
            if (!shouldPlay && boilingFade <= 0.001f && boilingLoopSource.isPlaying)
            {
                boilingLoopSource.Stop();
            }
        }

        private float GetOutputGain()
        {
            if (audioManager == null)
            {
                audioManager = AudioManager.Instance;
            }

            return audioManager != null ? audioManager.MasterVolume * audioManager.SfxVolume : 1f;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            EventManager.OnCustomerArrived += HandleCustomerArrived;
            EventManager.OnCustomerOrderPlaced += HandleOrderPlaced;
            EventManager.OnCustomerLeftSad += HandleCustomerLeftSad;
            EventManager.OnSaleCompleted += HandleSaleCompleted;
            EventManager.OnBusinessDayPhaseChanged += HandleBusinessDayPhaseChanged;
        }

        private void UnsubscribeEvents()
        {
            EventManager.OnCustomerArrived -= HandleCustomerArrived;
            EventManager.OnCustomerOrderPlaced -= HandleOrderPlaced;
            EventManager.OnCustomerLeftSad -= HandleCustomerLeftSad;
            EventManager.OnSaleCompleted -= HandleSaleCompleted;
            EventManager.OnBusinessDayPhaseChanged -= HandleBusinessDayPhaseChanged;
        }

        private void HandleCustomerArrived(NPCType type)
        {
            PlayCue(GameplaySfxCue.CustomerArrive);
        }

        private void HandleOrderPlaced(int drinkId, string drinkName)
        {
            StartCoroutine(PlayDelayed(GameplaySfxCue.OrderPlaced, 0.22f));
        }

        private void HandleCustomerLeftSad(NPCType type)
        {
            PlayCue(GameplaySfxCue.Error);
        }

        private void HandleSaleCompleted(int orderId, int amount)
        {
            PlayCue(GameplaySfxCue.Payment);
        }

        private void HandleBusinessDayPhaseChanged(BusinessDayPhase phase)
        {
            if (phase == BusinessDayPhase.Trading)
            {
                PlayCue(GameplaySfxCue.ShopOpen);
            }
            else if (phase == BusinessDayPhase.Closing || phase == BusinessDayPhase.AfterHours)
            {
                PlayCue(GameplaySfxCue.ShopClose);
            }
        }

        private IEnumerator PlayDelayed(GameplaySfxCue cue, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            PlayCue(cue);
        }

        private static CueSettings GetSettings(GameplaySfxCue cue)
        {
            switch (cue)
            {
                case GameplaySfxCue.CupPickup: return new CueSettings("cup_pickup", 0.62f, 0.97f, 1.04f, 0.08f);
                case GameplaySfxCue.CupPlace: return new CueSettings("cup_place", 0.58f, 0.96f, 1.03f, 0.08f);
                case GameplaySfxCue.PourWater: return new CueSettings("pour_water", 0.56f, 0.98f, 1.02f, 0.18f);
                case GameplaySfxCue.AddIngredient: return new CueSettings("add_ingredient", 0.48f, 0.94f, 1.06f, 0.08f);
                case GameplaySfxCue.AddIce: return new CueSettings("add_ice", 0.52f, 0.94f, 1.06f, 0.12f);
                case GameplaySfxCue.StoveIgnite: return new CueSettings("stove_ignite", 0.56f, 0.98f, 1.02f, 0.4f);
                case GameplaySfxCue.KettleReady: return new CueSettings("kettle_ready", 0.38f, 0.98f, 1.02f, 0.5f);
                case GameplaySfxCue.WashCup: return new CueSettings("wash_cup", 0.46f, 0.98f, 1.02f, 0.4f);
                case GameplaySfxCue.DrinkReady: return new CueSettings("drink_ready", 0.36f, 0.99f, 1.01f, 0.35f);
                case GameplaySfxCue.ServeSuccess: return new CueSettings("serve_success", 0.38f, 0.98f, 1.02f, 0.28f);
                case GameplaySfxCue.Payment: return new CueSettings("payment", 0.42f, 0.96f, 1.04f, 0.25f);
                case GameplaySfxCue.CustomerArrive: return new CueSettings("customer_arrive", 0.28f, 0.98f, 1.03f, 0.3f);
                case GameplaySfxCue.OrderPlaced: return new CueSettings("order_bell", 0.25f, 0.98f, 1.03f, 0.25f);
                case GameplaySfxCue.Error: return new CueSettings("error", 0.3f, 0.97f, 1.01f, 0.3f);
                case GameplaySfxCue.ShopOpen: return new CueSettings("shop_open", 0.32f, 0.99f, 1.01f, 0.5f);
                case GameplaySfxCue.ShopClose: return new CueSettings("shop_close", 0.32f, 0.99f, 1.01f, 0.5f);
                case GameplaySfxCue.MenuOpen: return new CueSettings("menu_open", 0.2f, 0.99f, 1.01f, 0.12f);
                case GameplaySfxCue.MenuClose: return new CueSettings("menu_close", 0.18f, 0.99f, 1.01f, 0.12f);
                default: return new CueSettings("error", 0.3f, 1f, 1f, 0.1f);
            }
        }

        private readonly struct CueSettings
        {
            public readonly string ResourceName;
            public readonly float Volume;
            public readonly float MinimumPitch;
            public readonly float MaximumPitch;
            public readonly float Cooldown;

            public CueSettings(string resourceName, float volume, float minimumPitch, float maximumPitch, float cooldown)
            {
                ResourceName = resourceName;
                Volume = volume;
                MinimumPitch = minimumPitch;
                MaximumPitch = maximumPitch;
                Cooldown = cooldown;
            }
        }
    }
}

using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Simple singleton sound manager for playing audio clips
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip plantDeathSound;
        [SerializeField] private AudioClip pollutionDestroyedSound;
        [SerializeField] private AudioClip graftingSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip victorySound;
        [SerializeField] private AudioClip defeatSound;

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Settings")]
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float musicVolume = 0.5f;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoad(gameObject);

            // Initialize audio sources if not assigned
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
            }

            UpdateVolumes();
        }

        private void Start()
        {
            PlayBackgroundMusic();
        }

        /// <summary>
        /// Updates the volume of audio sources
        /// </summary>
        private void UpdateVolumes()
        {
            if (musicSource != null)
                musicSource.volume = musicVolume;
            
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }

        /// <summary>
        /// Plays a sound effect once
        /// </summary>
        private void PlaySound(AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volumeScale);
            }
        }

        // Public methods to play specific sounds
        public void PlayPlantDeath()
        {
            PlaySound(plantDeathSound);
        }

        public void PlayPollutionDestroyed()
        {
            PlaySound(pollutionDestroyedSound);
        }

        public void PlayGrafting()
        {
            PlaySound(graftingSound);
        }

        public void PlayButtonClick()
        {
            PlaySound(buttonClickSound);
        }

        public void PlayError()
        {
            PlaySound(errorSound);
        }

        public void PlayVictory()
        {
            PlaySound(victorySound, 0.6f);
        }

        public void PlayDefeat()
        {
            PlaySound(defeatSound);
        }

        /// <summary>
        /// Plays the background music
        /// </summary>
        public void PlayBackgroundMusic()
        {
            if (backgroundMusic != null && musicSource != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Stops the background music
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Sets the SFX volume (0-1)
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }

        /// <summary>
        /// Sets the music volume (0-1)
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }
    }
}


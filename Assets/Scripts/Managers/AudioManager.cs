using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Overrailed.Managers.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public event System.Action OnVolumeChange;

        [SerializeField] [Range(0, 1)] private float masterVolume = 0.5f, soundVolume = 0.5f, musicVolume = 0.5f;
        [Space]
        [SerializeField] private AudioClip startingMusic;
        [SerializeField] private AudioGroup[] audioGroups;

        private Dictionary<string, AudioClip[]> audioDictionary;
        private List<AudioSource> soundSources;
        private AudioSource[] musicSources;
        private int activeMusicSourceIndex;

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = value;
                OnVolumeChange?.Invoke();
            }
        }
        public float SoundVolume
        {
            get => soundVolume;
            set
            {
                soundVolume = value;
                OnVolumeChange?.Invoke();
            }
        }
        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = value;
                OnVolumeChange?.Invoke();
            }
        }

        public static AudioManager instance;

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            OnVolumeChange += UpdateAudioSourceVolumes;

            audioDictionary = new Dictionary<string, AudioClip[]>();
            foreach (var group in audioGroups) audioDictionary.Add(group.Title, group.Clips);

            soundSources = new List<AudioSource>();
            for (int i = 0; i < 5; i++) AddSoundSource();

            musicSources = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                var musicSource = new GameObject("Music Source " + (i + 1));
                musicSources[i] = musicSource.AddComponent<AudioSource>();

                musicSource.transform.parent = transform;
            }
            if (startingMusic) PlayMusic(startingMusic, true);
        }

        private static void AddSoundSource()
        {
            var soundSource = new GameObject("Sound Source " + (instance.soundSources.Count + 1));
            soundSource.transform.parent = instance.transform;
            soundSource.transform.SetSiblingIndex(instance.soundSources.Count);

            instance.soundSources.Add(soundSource.AddComponent<AudioSource>());
        }

        private void UpdateAudioSourceVolumes()
        {
            foreach (var source in soundSources) source.volume = masterVolume * soundVolume;
            foreach (var source in musicSources) source.volume = masterVolume * musicVolume;
        }

        public static async void PlaySound(AudioClip clip, Vector3 position)
        {
            if (instance == null) return;

            if (clip)
            {
                AudioSource source = null;
                foreach (var s in instance.soundSources) if (s.clip == null) source = s;
                if (source == null)
                {
                    AddSoundSource();
                    source = instance.soundSources[instance.soundSources.Count - 1];
                }

                source.transform.position = position;
                source.clip = clip;
                source.Play();

                await Task.Delay(Mathf.CeilToInt(1000 * clip.length));

                if (source)
                {
                    source.transform.position = Vector3.zero;
                    source.clip = null;
                }
            }
            else Debug.LogWarning("Null clip given");
        }

        public void PlaySound(string title, Vector3 position)
        {
            if (audioDictionary.TryGetValue(title, out AudioClip[] clips)) PlaySound(clips[Random.Range(0, clips.Length)], position);
            else Debug.LogError("No audio group titled \"" + title + "\".");
        }

        public async void PlayMusic(AudioClip clip, bool loop, float fadeDuration = 0)
        {
            activeMusicSourceIndex = 1 - activeMusicSourceIndex;

            musicSources[activeMusicSourceIndex].clip = clip;
            musicSources[activeMusicSourceIndex].loop = loop;
            musicSources[activeMusicSourceIndex].Play();

            if (fadeDuration == 0)
            {
                musicSources[activeMusicSourceIndex].volume = masterVolume * musicVolume;
                musicSources[1 - activeMusicSourceIndex].Stop();
                return;
            }

            float percent = 0;
            while (percent < 1)
            {
                musicSources[activeMusicSourceIndex].volume = Mathf.Lerp(0, masterVolume * musicVolume, percent);
                musicSources[1 - activeMusicSourceIndex].volume = Mathf.Lerp(masterVolume * musicVolume, 0, percent);

                percent += Time.deltaTime / fadeDuration;
                await Task.Yield();
            }

            musicSources[1 - activeMusicSourceIndex].Stop();
        }

        private void OnValidate()
        {
            if (Time.time > 0 && instance == this) UpdateAudioSourceVolumes();
        }
        
        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        [System.Serializable]
        public class AudioGroup
        {
            [SerializeField] private string title;
            [SerializeField] private AudioClip[] clips;

            public string Title => title;
            public AudioClip[] Clips => clips;

            public AudioClip GetRandomClip() => clips[Random.Range(0, clips.Length)];
        }
    }
}
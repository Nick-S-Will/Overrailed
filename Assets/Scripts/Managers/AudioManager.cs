using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Overrailed.Managers.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public event System.Action OnVolumeChange;

        [SerializeField] private bool on = true;
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
            if (!on) return;

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
            if (startingMusic) _ = StartCoroutine(PlayMusic(startingMusic, true));
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

        public static IEnumerator PlaySound(AudioClip clip, Vector3 position)
        {
            if (instance == null) yield break;

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

                yield return new WaitForSeconds(clip.length);

                if (source)
                {
                    source.transform.position = Vector3.zero;
                    source.clip = null;
                }
            }
            else Debug.LogWarning("Null clip given");
        }

        public static void PlaySound(string title, Vector3 position)
        {
            if (instance == null) return;

            if (instance.audioDictionary.TryGetValue(title, out AudioClip[] clips)) _ = instance.StartCoroutine(PlaySound(clips[Random.Range(0, clips.Length)], position)); else Debug.LogError("No audio group titled \"" + title + "\".");
        }

        public static IEnumerator PlayMusic(AudioClip clip, bool loop, float fadeDuration = 0f)
        {
            if (instance == null) yield break;

            instance.activeMusicSourceIndex = 1 - instance.activeMusicSourceIndex;

            instance.musicSources[instance.activeMusicSourceIndex].clip = clip;
            instance.musicSources[instance.activeMusicSourceIndex].loop = loop;
            instance.musicSources[instance.activeMusicSourceIndex].Play();

            if (fadeDuration == 0)
            {
                instance.musicSources[instance.activeMusicSourceIndex].volume = instance.masterVolume * instance.musicVolume;
                instance.musicSources[1 - instance.activeMusicSourceIndex].Stop();
                yield break;
            }

            float percent = 0;
            while (percent < 1)
            {
                instance.musicSources[instance.activeMusicSourceIndex].volume = Mathf.Lerp(0, instance.masterVolume * instance.musicVolume, percent);
                instance.musicSources[1 - instance.activeMusicSourceIndex].volume = Mathf.Lerp(instance.masterVolume * instance.musicVolume, 0, percent);

                percent += Time.deltaTime / fadeDuration;
                yield return null;
            }

            instance.musicSources[1 - instance.activeMusicSourceIndex].Stop();
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
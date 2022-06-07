using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Overrailed.Managers
{
    public enum GameState { Play, Paused, Edit }

    public abstract class Manager : MonoBehaviour
    {
        public static Action OnPause, OnResume;

        [SerializeField] protected string titleSceneName = "TitleScreenScene", gameSceneName = "GameScene", tutorialSceneName = "TutorialScene";
        [SerializeField] private GameObject pauseMenuObject;

        private static TaskCompletionSource<bool> pauseCompletionSource;

        public static Manager instance;
        protected static MonoBehaviour skinPrefab;
        /// <summary>
        /// <see cref="PlayerPrefs"/> Key
        /// </summary>
        public readonly static string CurrentSkinIndexKey = "Current Skin", MasterVolumeKey = "Master Volume", SoundVolumeKey = "Sound Volume", MusicVolumeKey = "Music Volume", SeedKey = "Seed";

        public static MonoBehaviour GetSkin() => skinPrefab;
        public static Task Pause => pauseCompletionSource.Task;
        /// <summary>
        /// <see cref="Task.Delay(int)"/> but awaits <see cref="Pause"/>
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static async Task Delay(float seconds)
        {
            float time = 0f;
            while (time < seconds)
            {
                await Pause;
                await Task.Yield();
                time += Time.deltaTime;
            }
        }
        public static GameState CurrentState { get; protected set; }
        public static bool IsPlaying() => CurrentState == GameState.Play;
        public static bool IsPaused() => CurrentState == GameState.Paused;
        public static bool IsEditing() => CurrentState == GameState.Edit;
        public static bool Exists => instance;

        protected virtual void Awake()
        {
            if (instance)
            {
                Debug.LogError("Mutliple Managers Exist");
                return;
            }

            instance = this;
            CurrentState = GameState.Play;
            pauseCompletionSource = new TaskCompletionSource<bool>();
            pauseCompletionSource.SetResult(true);

            SetCursor(false);
            Pausing.HandlePausing(Keyboard.current.escapeKey);
        }

        /// <summary>
        /// Sets <see cref="Cursor.visible"/> to <paramref name="visible"/> and <see cref="Cursor.lockState"/> to <paramref name="visible"/> ? <see cref="CursorLockMode.None"/> : <see cref="CursorLockMode.Locked"/>
        /// </summary>
        public static void SetCursor(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        protected virtual void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        protected static class Pausing
        {
            private static bool isPaused = false, isForced = false;

            public static void ForcePause()
            {
                Pause();
                isForced = true;
            }
            public static void ForceResume()
            {
                isForced = false;
                Resume();
            }
            public static void Pause() => SetPaused(true);
            public static void Resume() => SetPaused(false);
            public static void TogglePause()
            {
                SetPaused(!isPaused);
                if (!isForced) instance.pauseMenuObject.SetActive(isPaused);
            }
            private static void SetPaused(bool paused)
            {
                if (isPaused == paused || isForced) return;

                if (paused)
                {
                    pauseCompletionSource = new TaskCompletionSource<bool>();
                    CurrentState = GameState.Paused;
                    
                    Utils.PauseTasks();
                    OnPause?.Invoke();
                }
                else
                {
                    pauseCompletionSource.SetResult(true);
                    CurrentState = GameState.Play;
                    
                    Utils.ResumeTasks();
                    OnResume?.Invoke();
                }

                isPaused = paused;
                SetCursor(paused);
            }

            public static async void HandlePausing(KeyControl pauseKey)
            {
                var startScene = SceneManager.GetActiveScene();

                while (Application.isPlaying && startScene == SceneManager.GetActiveScene())
                {
                    if (pauseKey.wasPressedThisFrame) TogglePause();
                    await Task.Yield();
                }
            }
        }
    }
}
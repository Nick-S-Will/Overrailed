using System;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Overrailed.Managers
{
    public enum GameState { Play, Paused, Edit }

    public abstract class Manager : MonoBehaviour
    {
        public static Action OnPause, OnResume;

        [SerializeField] protected SceneAsset titleScene, tutorialScene, gameScene;
        [SerializeField] private GameObject pauseMenuObject;

        private static TaskCompletionSource<bool> pauseCompletionSource;

        public static Manager instance;
        protected static MonoBehaviour skinPrefab;
        /// <summary>
        /// <see cref="PlayerPrefs"/> Key
        /// </summary>
        public readonly static string CurrentSkinIndexKey = "Current Skin", MasterVolumeKey = "Master Volume", SoundVolumeKey = "Sound Volume", MusicVolumeKey = "Music Volume", SeedKey = "Seed";

        public static MonoBehaviour GetSkin() => skinPrefab;
        /// <summary>
        /// Halts async functions until the game is no longer paused
        /// </summary>
        public static Task Pause => pauseCompletionSource.Task;
        /// <summary>
        /// Halts coroutine until the game is no longer paused
        /// </summary>
        public static WaitUntil PauseRoutine => new WaitUntil(() => Pause.IsCompleted);
        /// <summary>
        /// <see cref="Task.Delay(int)"/> but awaits <see cref="Pause"/>
        /// </summary>
        public static async Task Delay(float seconds)
        {
            float elapsedTime = 0f;
            while (elapsedTime < seconds)
            {
                float startTime = Time.time;
                await Pause;
                await Task.Yield();
                elapsedTime += Time.fixedDeltaTime + Time.time - startTime;
            }
        }
        public static IEnumerator DelayRoutine(float seconds)
        {
            float elapsedTime = 0f;
            while (elapsedTime < seconds)
            {
                float startTime = Time.time;
                yield return PauseRoutine;
                elapsedTime += Time.fixedDeltaTime + Time.time - startTime;
                yield return new WaitForSeconds(Time.fixedDeltaTime);
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
            Pausing.HandlePausing(Keyboard.current, Keyboard.current.escapeKey);
            if (Gamepad.current != null) Pausing.HandlePausing(Gamepad.current, Gamepad.current.startButton);
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

            public static async void HandlePausing(InputDevice device, ButtonControl pauseButton)
            {
                var startScene = SceneManager.GetActiveScene();

                while (Application.isPlaying && device != null && startScene == SceneManager.GetActiveScene())
                {
                    if (pauseButton.wasPressedThisFrame) TogglePause();
                    await Task.Yield();
                }
            }
        }
    }
}
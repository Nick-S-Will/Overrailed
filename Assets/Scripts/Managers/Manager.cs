using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

namespace Overrailed.Managers
{
    public enum GameState { Play, Paused, Edit }

    public abstract class Manager : MonoBehaviour
    {
        public static Action OnPause, OnResume;

        [SerializeField] protected string titleSceneName = "TitleScreenScene", tutorialSceneName = "TutorialScene", gameSceneName = "GameScene";
        [SerializeField] private GameObject pauseMenuObject;

        public static Manager instance;
        protected static MonoBehaviour skinPrefab;
        /// <summary>
        /// <see cref="PlayerPrefs"/> Key
        /// </summary>
        public readonly static string CurrentSkinIndexKey = "Current Skin", MasterVolumeKey = "Master Volume", SoundVolumeKey = "Sound Volume", MusicVolumeKey = "Music Volume", SeedKey = "Seed";

        public static MonoBehaviour GetSkin() => skinPrefab;
        /// <summary>
        /// Halts coroutines while the game is paused
        /// </summary>
        public static WaitWhile PauseRoutine => new WaitWhile(() => paused);
        /// <summary>
        /// <see cref="Task.Delay(int)"/> but awaits <see cref="Pause"/>
        /// </summary>
        /// 
        private static bool paused;
        
        protected virtual void Awake()
        {
            if (instance)
            {
                Debug.LogError("Mutliple Managers Exist");
                return;
            }

            instance = this;
            CurrentState = GameState.Play;

            SetCursor(false);
            _ = StartCoroutine(Pausing.HandlePausing(Keyboard.current, Keyboard.current.escapeKey));
            if (Gamepad.current != null) _ = StartCoroutine(Pausing.HandlePausing(Gamepad.current, Gamepad.current.startButton));
        }

        public static IEnumerator Delay(float seconds)
        {
            float elapsedSeconds = 0f;
            while (elapsedSeconds < seconds)
            {
                float startTime = Time.time;
                yield return PauseRoutine;
                yield return null;
                elapsedSeconds += Time.time - startTime;
            }
        }
        public static IEnumerator DelayRoutine(float seconds)
        {
            float elapsedTime = 0f;
            while (elapsedTime < seconds)
            {
                float startTime = Time.time;
                yield return PauseRoutine;
                yield return new WaitForSeconds(Time.fixedDeltaTime);
                elapsedTime += Time.time - startTime;
            }
        }
        public static GameState CurrentState { get; protected set; }
        public static bool IsPlaying() => CurrentState == GameState.Play;
        public static bool IsPaused() => CurrentState == GameState.Paused;
        public static bool IsEditing() => CurrentState == GameState.Edit;
        public static bool Exists => instance;

        /// <summary>
        /// Sets <see cref="Cursor.visible"/> to <paramref name="visible"/> and <see cref="Cursor.lockState"/> to <paramref name="visible"/> ? <see cref="CursorLockMode.None"/> : <see cref="CursorLockMode.Locked"/>
        /// </summary>
        public static void SetCursor(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void QuitGame() // Used by trigger button
        {
            Application.Quit();
            Debug.Log("Quit");
        }

        protected virtual void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        protected static class Pausing
        {
            private static GameState prevState = GameState.Play;
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
                    Manager.paused = true;
                    if (CurrentState != GameState.Paused) prevState = CurrentState;
                    CurrentState = GameState.Paused;

                    OnPause?.Invoke();
                }
                else
                {
                    Manager.paused = false;
                    CurrentState = prevState;

                    OnResume?.Invoke();
                }

                isPaused = paused;
                SetCursor(paused);
            }

            public static IEnumerator HandlePausing(InputDevice device, ButtonControl pauseButton)
            {
                var startScene = SceneManager.GetActiveScene();

                while (Application.isPlaying && device != null && startScene == SceneManager.GetActiveScene())
                {
                    if (pauseButton.wasPressedThisFrame) TogglePause();
                    yield return null;
                }
            }
        }
    }
}
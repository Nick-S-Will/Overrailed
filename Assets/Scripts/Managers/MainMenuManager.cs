using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

using Overrailed.Managers.Audio;
using Overrailed.Player;
using Overrailed.UI;

namespace Overrailed.Managers
{
    public class MainMenuManager : Manager
    {
        #region Inspector Variables
        [Space]
        [SerializeField] private Camera cam;
        [SerializeField] private Transform spawnPoint;
        [Header("Main Menu Settings")]
        [SerializeField] private Transform mainTitle;
        [SerializeField] private Transform controls;
        [Space]
        [SerializeField] private TriggerButton playButton;
        [SerializeField] private TriggerButton tutorialButton, optionsButton;
        [Space]
        [SerializeField] private TriggerButton quitButton;
        [SerializeField] private Transform mainCamTransform;
        [SerializeField] private Vector3 mainSlideOffset = 10 * Vector3.forward;
        [Header("Options Menu Settings")]
        [SerializeField] private Transform optionsTitle;
        [SerializeField] private TriggerButton skinsButton;
        [Space]
        [SerializeField] private TriggerButton volumeButton;
        [SerializeField] private Slider[] volumeSliders;
        [SerializeField] private ClickButton volumeSaveButton;
        [Space]
        [SerializeField] private TriggerButton seedButton;
        [SerializeField] private TextMeshPro seedText;
        [SerializeField] private ClickButton seedSaveButton;
        [SerializeField] private int maxSeedLength = 5;
        [Space]
        [SerializeField] private TriggerButton returnButton;
        [SerializeField] private Vector3 optionsSlideOffset = 10 * Vector3.right;
        [Header("Skins Menu Settings")]
        [SerializeField] private GameObject skinCanvasParent;
        [SerializeField] private Transform skinCamTransform, skinsParent;
        [SerializeField] private Vector3 spaceBetweenSkins = 2 * Vector3.right;
        [SerializeField] private float skinTurnSpeed = 5;
        [Header("Slide Settings")]
        [SerializeField] [Min(1)] private float slideSpeed = 100;
        [SerializeField] [Min(0)] private float elementSlideInterval = 0.2f;
        [Header("Cam Settings")]
        [SerializeField] private float camMoveSpeed = 10;
        [SerializeField] private float camAngularSpeed = 45;
        [Space]
        [SerializeField] private PlayerController[] skinPrefabs;
        #endregion

        private Transform[] mainElements, optionsElements, volumeElements, seedElements;
        private PlayerController player;
        private Coroutine rotateSkins;
        private Vector3 playerLastPosition;
        private int currentSkinIndex;

        protected override void Awake() => base.Awake();

        void Start()
        {
            currentSkinIndex = PlayerPrefs.GetInt(CurrentSkinIndexKey, 0);
            skinPrefab = skinPrefabs[currentSkinIndex];

            mainElements = new Transform[] { mainTitle, quitButton.transform, optionsButton.transform, tutorialButton.transform, playButton.transform, controls };
            optionsElements = new Transform[] { optionsTitle, returnButton.transform, seedButton.transform, volumeButton.transform, skinsButton.transform };
            volumeElements = new Transform[] { volumeSliders[0].transform.parent, volumeSliders[1].transform.parent, volumeSliders[2].transform.parent, volumeSaveButton.transform };
            seedElements = new Transform[] { seedText.transform, seedSaveButton.transform };

            foreach (var element in mainElements) element.position += mainSlideOffset;
            foreach (var element in optionsElements) element.position += optionsSlideOffset;
            foreach (var element in volumeElements) element.position += optionsSlideOffset;
            foreach (var element in seedElements) element.position += optionsSlideOffset;
        }

        #region Slide UI
        public void SlideInMainElements() => _ = StartCoroutine(SlideElementsRoutine(-mainSlideOffset, mainElements));
        private IEnumerator SlideElementsRoutine(Vector3 slideDelta, params Transform[] elements)
        {
            if (elements.Length == 0) yield break;

            foreach (var element in elements)
            {
                _ = StartCoroutine(SlideElementRoutine(slideDelta, element));
                yield return new WaitForSeconds(elementSlideInterval);
            }
        }
        private IEnumerator SlideElementRoutine(Vector3 slideDelta, Transform element)
        {
            var endPos = element.transform.position + slideDelta;

            while (element.position != endPos)
            {
                element.position = Vector3.MoveTowards(element.position, endPos, slideSpeed * Time.deltaTime);

                yield return null;
            }
        }
        #endregion

        #region Options Menu
        private IEnumerator SlideToOptionsRoutine()
        {
            yield return StartCoroutine(SlideElementsRoutine(-optionsSlideOffset, mainElements));
            yield return StartCoroutine(SlideElementsRoutine(-optionsSlideOffset, optionsElements));
        }
        public void SlideToOptions() // Used by trigger button
        {
            _ = StartCoroutine(SlideToOptionsRoutine());
        }

        private IEnumerator SlideFromOptionsRoutine()
        {
            yield return StartCoroutine(SlideElementsRoutine(optionsSlideOffset, optionsElements));
            yield return StartCoroutine(SlideElementsRoutine(optionsSlideOffset, mainElements));
        }
        public void SlideFromOptions() // Used by trigger button
        {
            _ = StartCoroutine(SlideFromOptionsRoutine());
        }

        #region Volume
        private IEnumerator UpdateVolumes()
        {
            var audioManager = FindObjectOfType<AudioManager>();
            volumeSliders[0].WriteValue(PlayerPrefs.GetFloat(MasterVolumeKey, 0.5f));
            volumeSliders[1].WriteValue(PlayerPrefs.GetFloat(SoundVolumeKey, 0.5f));
            volumeSliders[2].WriteValue(PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f));

            while (!player.enabled)
            {
                audioManager.MasterVolume = volumeSliders[0].ReadValue();
                audioManager.SoundVolume = volumeSliders[1].ReadValue();
                audioManager.MusicVolume = volumeSliders[2].ReadValue();

                yield return null;
            }
        }
        public void SlideInVolumeSliders() // Used by trigger button
        {
            player.DisableControls();
            _ = StartCoroutine(SlideElementsRoutine(-optionsSlideOffset, volumeElements));
            Slider.StartClickCheck(this, cam, Mouse.current);
            ClickButton.StartClickCheck(this, cam, Mouse.current);

            Pausing.ForcePause();
            StartCoroutine(UpdateVolumes());
        }
        public void SlideOutVolumeSliders() // Used by button
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, volumeSliders[0].ReadValue());
            PlayerPrefs.SetFloat(SoundVolumeKey, volumeSliders[1].ReadValue());
            PlayerPrefs.SetFloat(MusicVolumeKey, volumeSliders[2].ReadValue());

            ClickButton.StopClickCheck(this);
            Slider.StopClickCheck(this);
            _ = StartCoroutine(SlideElementsRoutine(optionsSlideOffset, volumeElements));
            player.EnableControls();

            Pausing.ForceResume();
        }
        #endregion

        #region Seed
        private IEnumerator UpdateSeedTextRoutine()
        {
            seedText.text = PlayerPrefs.GetString(SeedKey, "00000");

            while (this && !player.enabled)
            {
                var currentSeed = seedText.text;
                var newSeed = new StringBuilder(currentSeed, maxSeedLength);
                if (Keyboard.current.backspaceKey.wasPressedThisFrame && newSeed.Length > 0) newSeed.Remove(currentSeed.Length - 1, 1);
                if (newSeed.Length < maxSeedLength) Utils.AddNumbersThisFrame(ref newSeed);
                seedText.text = newSeed.ToString();

                yield return null;
            }
        }
        public void SlideInSeedMenu() // Used by trigger button
        {
            player.DisableControls();
            _ = StartCoroutine(SlideElementsRoutine(-optionsSlideOffset, seedElements));
            ClickButton.StartClickCheck(this, cam, Mouse.current);

            Pausing.ForcePause();

            _ = StartCoroutine(UpdateSeedTextRoutine());
        }
        public void SlideOutSeedMenu() // Used by button
        {
            PlayerPrefs.SetString(SeedKey, seedText.text);

            ClickButton.StopClickCheck(this);
            _ = StartCoroutine(SlideElementsRoutine(optionsSlideOffset, seedElements));
            player.EnableControls();

            Pausing.ForceResume();
        }
        #endregion

        #region Skins Menu
        public void MoveSelectedSkinToGame()
        {
            player = skinsParent.GetChild(currentSkinIndex).GetComponentInChildren<PlayerController>();
            player.transform.parent = null;
            player.transform.position = spawnPoint.position;
            player.transform.localRotation = Quaternion.identity;
            player.EnableControls();
        }

        private IEnumerator SwapToSkinsMenuRoutine()
        {
            player.DisableControls();

            _ = StartCoroutine(SlideElementsRoutine(-optionsSlideOffset, optionsElements));
            yield return StartCoroutine(Utils.MoveTransformTo(cam.transform, skinCamTransform, camMoveSpeed, camAngularSpeed));

            playerLastPosition = player.transform.position;
            player.transform.parent = skinsParent.GetChild(currentSkinIndex);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;

            skinsParent.localPosition = -spaceBetweenSkins * currentSkinIndex;
            skinsParent.gameObject.SetActive(true);
            skinCanvasParent.SetActive(true);
            rotateSkins = StartCoroutine(RotateSkins());

            Pausing.ForcePause();
        }
        public void SwapToSkinsMenu() // Used by trigger button
        {
            _ = StartCoroutine(SwapToSkinsMenuRoutine());
        }

        private IEnumerator SwapFromSkinsMenuRoutine()
        {
            StopCoroutine(rotateSkins);
            skinCanvasParent.SetActive(false);
            skinsParent.gameObject.SetActive(false);

            player = skinsParent.GetChild(currentSkinIndex).GetComponentInChildren<PlayerController>();
            PlayerPrefs.SetInt(CurrentSkinIndexKey, currentSkinIndex);
            skinPrefab = skinPrefabs[currentSkinIndex];

            Pausing.ForceResume();
            _ = StartCoroutine(SlideElementsRoutine(optionsSlideOffset, optionsElements));
            yield return _ = StartCoroutine(Utils.MoveTransformTo(cam.transform, mainCamTransform, camMoveSpeed, camAngularSpeed));

            player.transform.parent = null;
            player.transform.position = playerLastPosition;
            player.transform.localRotation = Quaternion.identity;
            player.EnableControls();
        }
        public void SwapFromSkinsMenu() // Used by canvas button
        {
            _ = StartCoroutine(SwapFromSkinsMenuRoutine());
        }

        private IEnumerator RotateSkins()
        {
            var yRotation = 0f;

            while (this)
            {
                yRotation += skinTurnSpeed * Time.deltaTime;
                var rotation = Quaternion.Euler(0, yRotation, 0);
                foreach (Transform t in skinsParent) t.GetChild(0).localRotation = rotation;

                yield return null;
            }
        }

        public void ScrollSkinMenuLeft() // Used by canvas button
        {
            if (currentSkinIndex == 0) return;

            _ = StartCoroutine(SlideElementRoutine(spaceBetweenSkins, skinsParent));
            currentSkinIndex--;
        }
        public void ScrollSkinMenuRight() // Used by canvas button
        {
            if (currentSkinIndex == skinsParent.childCount - 1) return;

            _ = StartCoroutine(SlideElementRoutine(-spaceBetweenSkins, skinsParent));
            currentSkinIndex++;
        }
        #endregion
        #endregion

        public void PlayGame() // Used by trigger button
        {
            SceneManager.LoadScene(gameSceneName);
        }

        public void PlayTutorial() // Used by trigger button
        {
            SceneManager.LoadScene(tutorialSceneName);
        }

        protected override void OnDestroy() => base.OnDestroy();
    }
}
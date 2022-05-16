using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

using Overrailed.Terrain.Generation;
using Overrailed.Player;
using Overrailed.UI;

namespace Overrailed.Managers
{
    public class MainMenuManager : MonoBehaviour
    {
        #region Inspector Variables
        [SerializeField] private string gameSceneName = "GameScene", tutorialSceneName = "TutorialScene";
        [Space]
        [SerializeField] private PlayerController player;
        [SerializeField] private MapManager map;
        [SerializeField] private Camera cam;
        [Header("Main Menu Settings")]
        [SerializeField] private Transform mainTitle;
        [SerializeField] private Transform controls;
        [Space]
        [SerializeField] private TriggerButton playButton;
        [SerializeField] private TriggerButton tutorialButton, optionsButton;
        [SerializeField] private Vector3 mainSlideOffset = 10 * Vector3.forward;
        [Space]
        [SerializeField] private Transform mainCamTransform;
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
        [Space]
        [SerializeField] private TriggerButton returnButton;
        [SerializeField] private Vector3 optionsSlideOffset = 10 * Vector3.right;
        [Header("Skins Menu Settings")]
        [SerializeField] private GameObject skinMenuCanvasParent;
        [SerializeField] private Transform skinCamTransform, skinsParent;
        [SerializeField] private Vector3 spaceBetweenSkins = 2 * Vector3.right;
        [SerializeField] private float skinTurnSpeed = 5;
        [Header("Slide Settings")]
        [SerializeField] [Min(1)] private float slideSpeed = 100;
        [SerializeField] [Min(0)] private float elementSlideInterval = 0.2f;
        [Header("Cam Settings")]
        [SerializeField] private float camMoveSpeed = 10;
        [SerializeField] private float camAngularSpeed = 45;
        #endregion

        private Transform[] mainElements, optionsElements, volumeElements, seedElements;
        private Coroutine rotateSkins;
        private Vector3 playerLastPosition;
        private int currentSkin = 0;

        public static bool Exists { get; private set; }

        void Awake()
        {
            Exists = true;
        }

        void Start()
        {
            playButton.OnPress += PlayGame;
            tutorialButton.OnPress += PlayTutorial;
            optionsButton.OnPress += SlideToOptions;

            skinsButton.OnPress += SwapToSkinsMenu;
            volumeButton.OnPress += SlideInVolumeSliders;
            seedButton.OnPress += SlideInSeedSliders;
            returnButton.OnPress += SlideFromOptions;

            volumeSaveButton.OnClick += SlideOutVolumeSliders;
            seedSaveButton.OnClick += SlideOutSeedSliders;

            mainElements = new Transform[] { mainTitle, optionsButton.transform, tutorialButton.transform, playButton.transform, controls };
            optionsElements = new Transform[] { optionsTitle, returnButton.transform, seedButton.transform, volumeButton.transform, skinsButton.transform };
            volumeElements = new Transform[] { volumeSliders[0].transform.parent, volumeSliders[1].transform.parent, volumeSliders[2].transform.parent, volumeSaveButton.transform };
            seedElements = new Transform[] { seedText.transform, seedSaveButton.transform };

            foreach (var element in mainElements) element.position += mainSlideOffset;
            foreach (var element in optionsElements) element.position += optionsSlideOffset;
            foreach (var element in volumeElements) element.position += optionsSlideOffset;
            foreach (var element in seedElements) element.position += optionsSlideOffset;

            map.OnFinishAnimateChunk += StartSlideMainElements;
        }

        private IEnumerator MoveCamTo(Transform transform)
        {
            while (this && (cam.transform.position != transform.position || cam.transform.rotation != transform.rotation))
            {
                cam.transform.position = Vector3.MoveTowards(cam.transform.position, transform.position, camMoveSpeed * Time.deltaTime);
                cam.transform.rotation = Quaternion.RotateTowards(cam.transform.rotation, transform.rotation, camAngularSpeed * Time.deltaTime);

                yield return null;
            }
        }

        #region Slide UI
        private void StartSlideMainElements() => _ = StartCoroutine(SlideElementsRoutine(-mainSlideOffset, mainElements));
        private IEnumerator SlideElementsRoutine(Vector3 slideDelta, params Transform[] elements)
        {
            if (elements.Length == 0) yield break;

            foreach (var element in elements)
            {
                _ = StartCoroutine(SlideElement(slideDelta, element));
                yield return new WaitForSeconds(elementSlideInterval);
            }
        }
        private IEnumerator SlideElement(Vector3 slideDelta, Transform element)
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
        private void SlideToOptions() => _ = StartCoroutine(SlideToOptionsRoutine());
        private void SlideFromOptions() => _ = StartCoroutine(SlideFromOptionsRoutine());

        private IEnumerator SlideToOptionsRoutine()
        {
            yield return SlideElementsRoutine(-optionsSlideOffset, mainElements);
            yield return SlideElementsRoutine(-optionsSlideOffset, optionsElements);
        }
        private IEnumerator SlideFromOptionsRoutine()
        {
            yield return SlideElementsRoutine(optionsSlideOffset, optionsElements);
            yield return SlideElementsRoutine(optionsSlideOffset, mainElements);
        }

        private IEnumerator UpdateVolumes()
        {
            var audioManager = FindObjectOfType<AudioManager>();

            while (!player.enabled)
            {
                audioManager.MasterVolume = volumeSliders[0].ReadValue();
                audioManager.SoundVolume = volumeSliders[1].ReadValue();
                audioManager.MusicVolume = volumeSliders[2].ReadValue();

                yield return null;
            }
        }
        private void SlideInVolumeSliders()
        {
            player.DisableControls();
            _ = StartCoroutine(SlideElementsRoutine(-optionsSlideOffset, volumeElements));
            Slider.StartClickCheck(this, cam, Mouse.current);
            ClickButton.StartClickCheck(this, cam, Mouse.current);

            _ = StartCoroutine(UpdateVolumes());
        }
        private void SlideOutVolumeSliders()
        {
            ClickButton.StopClickCheck(this);
            Slider.StopClickCheck(this);
            _ = StartCoroutine(SlideElementsRoutine(optionsSlideOffset, volumeElements));
            player.EnableControls();
        }

        private void SlideInSeedSliders()
        {
            player.DisableControls();
            _ = StartCoroutine(SlideElementsRoutine(-optionsSlideOffset, seedElements));
            ClickButton.StartClickCheck(this, cam, Mouse.current);
        }
        private void SlideOutSeedSliders()
        {
            ClickButton.StopClickCheck(this);
            _ = StartCoroutine(SlideElementsRoutine(optionsSlideOffset, seedElements));
            player.EnableControls();
        }
        #endregion

        #region Skins Menu
        public void SwapToSkinsMenu() => _ = StartCoroutine(SwapToSkinsMenuRoutine());
        private IEnumerator SwapToSkinsMenuRoutine()
        {
            player.DisableControls();

            _ = StartCoroutine(SlideElementsRoutine(-optionsSlideOffset, optionsElements));
            yield return StartCoroutine(MoveCamTo(skinCamTransform));

            playerLastPosition = player.transform.position;
            player.transform.parent = skinsParent.GetChild(currentSkin);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;

            skinsParent.gameObject.SetActive(true);
            skinMenuCanvasParent.SetActive(true);
            rotateSkins = StartCoroutine(RotateSkins());
        }
        public void SwapFromSkinsMenu() => _ = StartCoroutine(SwapFromSkinsMenuRoutine()); // Used by canvas button
        private IEnumerator SwapFromSkinsMenuRoutine()
        {
            StopCoroutine(rotateSkins);
            skinMenuCanvasParent.SetActive(false);
            skinsParent.gameObject.SetActive(false);

            player = skinsParent.GetChild(currentSkin).GetComponentInChildren<PlayerController>();

            _ = StartCoroutine(SlideElementsRoutine(optionsSlideOffset, optionsElements));
            yield return MoveCamTo(mainCamTransform);

            player.transform.parent = null;
            player.transform.position = playerLastPosition;
            player.transform.localRotation = Quaternion.identity;

            player.EnableControls();
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
            if (currentSkin == 0) return;

            _ = StartCoroutine(SlideElement(spaceBetweenSkins, skinsParent));
            currentSkin--;
        }
        public void ScrollSkinMenuRight() // Used by canvas button
        {
            if (currentSkin == skinsParent.childCount - 1) return;

            _ = StartCoroutine(SlideElement(-spaceBetweenSkins, skinsParent));
            currentSkin++;
        }
        #endregion

        private void PlayGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private void PlayTutorial()
        {
            SceneManager.LoadScene(tutorialSceneName);
        }

        void OnDestroy()
        {
            Exists = false;
        }
    }
}
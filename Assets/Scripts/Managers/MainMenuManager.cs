using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Overrailed.Terrain.Generation;
using Overrailed.Player;
using Overrailed.UI;

namespace Overrailed.Managers
{
    public class MainMenuManager : MonoBehaviour
    {
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
        [Space]
        [SerializeField] private TriggerButton skinsButton;
        [SerializeField] private TriggerButton volumeButton, seedButton, returnButton;
        [SerializeField] private Vector3 optionsSlideOffset = 10 * Vector3.right;
        [Header("Skins Menu Settings")]
        [SerializeField] private Button doneButton;
        [SerializeField] private Transform skinCamTransform;
        [Header("Slide Settings")]
        [SerializeField] [Min(1)] private float slideSpeed = 100;
        [SerializeField] [Min(0)] private float elementSlideInterval = 0.2f;
        [Header("Cam Settings")]
        [SerializeField] private float camMoveSpeed = 10;
        [SerializeField] private float camAngularSpeed = 45;

        private Transform[] mainElements, optionsElements;

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
            returnButton.OnPress += SlideFromOptions;

            mainElements = new Transform[] { mainTitle, controls, optionsButton.transform, tutorialButton.transform, playButton.transform };
            optionsElements = new Transform[] { optionsTitle, returnButton.transform, seedButton.transform, volumeButton.transform, skinsButton.transform };

            foreach (var element in mainElements) element.position += mainSlideOffset;
            foreach (var element in optionsElements) element.position += optionsSlideOffset;

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
        #endregion

        #region Skins Menu
        public void SwapToSkinsMenu() => _ = StartCoroutine(SwapToSkinsMenuRoutine());
        private IEnumerator SwapToSkinsMenuRoutine()
        {
            player.DisableControls();

            _ = StartCoroutine(SlideElementsRoutine(-optionsSlideOffset, optionsElements));
            yield return StartCoroutine(MoveCamTo(skinCamTransform));

            doneButton.gameObject.SetActive(true);
        }
        // Used by canvas button
        public void SwapFromSkinsMenu() => _ = StartCoroutine(SwapFromSkinsMenuRoutine());
        private IEnumerator SwapFromSkinsMenuRoutine()
        {
            doneButton.gameObject.SetActive(false);

            _ = StartCoroutine(SlideElementsRoutine(optionsSlideOffset, optionsElements));
            yield return MoveCamTo(mainCamTransform);

            player.EnableControls();
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
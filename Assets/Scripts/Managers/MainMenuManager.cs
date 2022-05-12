using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Overrailed.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene", tutorialSceneName = "TutorialScene";
    [Space]
    [SerializeField] private TriggerButton play;
    [SerializeField] private TriggerButton tutorial, options;

    void Start()
    {
        play.OnPress += PlayGame;
        tutorial.OnPress += PlayTutorial;
        options.OnPress += GoToOptions;
    }

    private void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void PlayTutorial()
    {
        SceneManager.LoadScene(tutorialSceneName);
    }

    private void GoToOptions()
    {

    }
}
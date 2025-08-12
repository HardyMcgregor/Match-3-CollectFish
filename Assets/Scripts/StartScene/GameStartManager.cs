using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    // Static variables to communicate auto-play mode to the game scene
    public static bool isAutoPlay = false;
    public static bool isAutoWin = false;

    public void LoadGameScene()
    {
        isAutoPlay = false;
        SceneManager.LoadScene("GameScene");
    }

    public void LoadGameSceneAutoWin()
    {
        Debug.Log("Starting Auto-Win mode...");
        isAutoPlay = true;
        isAutoWin = true;
        SceneManager.LoadScene("GameScene");
    }

    public void LoadGameSceneAutoLose()
    {
        Debug.Log("Starting Auto-Lose mode...");
        isAutoPlay = true;
        isAutoWin = false;
        SceneManager.LoadScene("GameScene");
    }

    // Reset static variables when the game starts normally
    void Awake()
    {
        // Only reset if we're in the start scene
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            isAutoPlay = false;
            isAutoWin = false;
        }
    }
}
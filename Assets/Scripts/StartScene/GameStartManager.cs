using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    public static bool isAutoPlay = false;
    public static bool isAutoWin = false;

    public void LoadGameScene()
    {
        isAutoPlay = false;
        isAutoWin = false;
        SceneManager.LoadScene("GameScene");
    }

    public void LoadGameSceneAutoWin()
    {
        isAutoPlay = true;
        isAutoWin = true;
        SceneManager.LoadScene("GameScene");
    }

    public void LoadGameSceneAutoLose()
    {
        isAutoPlay = true;
        isAutoWin = false;
        SceneManager.LoadScene("GameScene");
    }

    void Awake()
    {
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            isAutoPlay = false;
            isAutoWin = false;
        }
    }
}
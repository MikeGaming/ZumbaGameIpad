using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStarter : MonoBehaviour
{
    [SerializeField] GameObject assetToLoad;

    GameObject startButtonCanvas;

    private void Awake()
    {
        assetToLoad.SetActive(false);
        startButtonCanvas = GetComponentInChildren<Canvas>().gameObject;
        Time.timeScale = 0f;
    }

    public void StartScene()
    {
        Time.timeScale = 1f;
        startButtonCanvas.SetActive(false);
        assetToLoad.SetActive(true);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_SWITCH && !UNITY_EDITOR
using _Assets._PlatformSpeciffics.Switch;
#endif

public class Init : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("INIT SCENE AWAKE!");
#if UNITY_SWITCH && !UNITY_EDITOR
        NintendoSave.Initialize();
#endif
    }

    private void Start()
    {
        //LoadingScreen.LoadScene("Menu");
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }

}

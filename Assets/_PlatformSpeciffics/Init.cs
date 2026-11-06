using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Assets._PlatformSpeciffics.Switch;

public class Init : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("INIT SCENE AWAKE!");
#if UNITY_EDITOR
        Debug.Log("Nintendo Initialize!");
#else
        NintendoSave.Initialize();
#endif
    }

    private void Start()
    {
        //LoadingScreen.LoadScene("Menu");
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }

}

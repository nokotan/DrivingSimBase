using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CaseTransitionManager : MonoBehaviour
{
    List<KeyCode> sceneMapping;

    // Start is called before the first frame update
    void Start()
    {
        sceneMapping = new List<KeyCode>()
        {
            KeyCode.Alpha0,
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8
        };

        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Mathf.Min(SceneManager.sceneCountInBuildSettings, sceneMapping.Count); i++)
        {
            if (Input.GetKeyDown(sceneMapping[i]))
            {
                SceneManager.LoadScene(i);
            }
        }
    }
}

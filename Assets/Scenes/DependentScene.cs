using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class DependentScene : MonoBehaviour
{
    [SerializeField]
    string[] parentScenes;

    bool FindSceneExistence(string sceneName)
    {
        Scene checkedScene = SceneManager.GetSceneByPath($"Assets/{sceneName}.unity");

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            Debug.Log($"Loaded Scene is {s.name}, {checkedScene.name}");
            
            if (s == checkedScene)
            {
                return true;
            }
        }

        return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (var name in parentScenes)
        {
            if (!FindSceneExistence(name))
            {              
                if (Application.IsPlaying(this))
                {
                    SceneManager.LoadScene(name, LoadSceneMode.Additive);
                }
#if UNITY_EDITOR
                else
                {
                    EditorSceneManager.OpenScene($"Assets/{name}.unity", OpenSceneMode.Additive);
                }
#endif
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

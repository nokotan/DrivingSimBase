using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

[ExecuteAlways]
public class DependentScene : MonoBehaviour
{
    [SerializeField]
    string[] parentScenes;
    
    bool FindSceneExistence(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);

            if (s.name == sceneName)
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
                else
                {
                    EditorSceneManager.OpenScene($"Assets/{name}.unity", OpenSceneMode.Additive);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

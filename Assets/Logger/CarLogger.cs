using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Vehicles.Car;

public class CarLogger : MonoBehaviour
{
    static string SavedFolder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SharedNew()
    {
        SavedFolder = Path.Combine(Application.streamingAssetsPath, DateTime.Now.ToString("yy_MM_dd hh_mm"));
        Directory.CreateDirectory(SavedFolder);
    }

    [SerializeField] string FolderName;
    [SerializeField] CarController[] carController;

    StreamWriter streamWriter;

    // Start is called before the first frame update
    void Awake()
    {
        var caseFileName = Path.Combine(SavedFolder, FolderName);
        var caseFolderName = Path.GetDirectoryName(caseFileName);

        if (!Directory.Exists(caseFolderName))
        {
            Directory.CreateDirectory(caseFolderName);
        }

        streamWriter = new StreamWriter(caseFileName, append: false);
        WriteHeader();
    }

    void OnDestroy()
    {
        streamWriter.Close();
    }

    void WriteHeader()
    {
        var columns = new List<string>() { "Time" };

        foreach (var item in carController)
        {
            var objectName = item.gameObject.name;

            columns.Add($"{objectName}.Z");
            columns.Add($"{objectName}.X");
            columns.Add($"{objectName}.Speed");
        }

        columns.Add("\n");

        streamWriter.WriteAsync(string.Join(",", columns));
    }

    void Record()
    {
        var columns = new List<string>() { LastRecordedTime.ToString() };

        foreach (var item in carController)
        {
            var objectName = item.gameObject.ToString();

            columns.Add(item.transform.position.z.ToString());
            columns.Add(item.transform.position.x.ToString());
            columns.Add(item.CurrentSpeed.ToString());
        }

        columns.Add("\n");

        streamWriter.WriteAsync(string.Join(",", columns));
    }

    int LastRecordedTime = -1;

    // Update is called once per frame
    void Update()
    {
        int CurrentTime = Mathf.FloorToInt(Time.time);

        if (LastRecordedTime != CurrentTime)
        {
            LastRecordedTime = CurrentTime;
            Record();
        }
    }
}

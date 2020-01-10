using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ScreenActivator
{
    [RuntimeInitializeOnLoadMethod()]
    public static void OnStart()
    {
        int count = Mathf.Min(Display.displays.Length, 3);

        for (int i = 0; i < count; ++i)
        {
            Display.displays[i].Activate();
        }
    }
}

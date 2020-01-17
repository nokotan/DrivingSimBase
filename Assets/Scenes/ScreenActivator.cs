﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ScreenActivator
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public static void OnStart()
    {
        int count = Mathf.Min(Display.displays.Length, 3);

        for (int i = 0; i < count; ++i)
        {
            Display.displays[i].Activate();

            if (i == 0)
            {
                Display.displays[i].SetRenderingResolution(1920, 1280);
            }
            else
            {
                Display.displays[i].SetRenderingResolution(1440, 900);
            }
        }
    }
}

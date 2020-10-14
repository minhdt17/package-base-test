using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ZBaseEditorCoroutines : MonoBehaviour
{
    readonly IEnumerator mRoutine;

    public static ZBaseEditorCoroutines StartEditorCoroutine(IEnumerator routine)
    {
        ZBaseEditorCoroutines coroutine = new ZBaseEditorCoroutines(routine);
        coroutine.start();
        return coroutine;
    }

    ZBaseEditorCoroutines(IEnumerator routine)
    {
        mRoutine = routine;
    }

    void start()
    {
        EditorApplication.update += update;
    }

    void update()
    {
        if (!mRoutine.MoveNext())
        {
            StopEditorCoroutine();
        }
    }

    public void StopEditorCoroutine()
    {
        EditorApplication.update -= update;
    }
}

using UnityEditor;
using UnityEngine;

public class ZitgaBaseManagerMenu
{
    [MenuItem("ZitgaBase/Tool Manager", false, 0)]
    public static void ToolManager()
    {
        //Debug.Log("Tool Manager");
        ZBaseDependenciesManager.ShowZBaseDependenciesManager();
    }
}

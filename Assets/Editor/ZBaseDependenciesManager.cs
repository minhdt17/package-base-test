using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using ZBaseJsonHelper;

public class ZBaseDependenciesManager : EditorWindow
{
    private const string packVersionURL = "https://github.com/minhdt17/package-base-test/raw/main/Packages/com.zitga.packagetest/package.json";
    private const string packCurrVersionDir = "Packages/packages-lock.json";
    private const int Width = 760;
    private const int Height = 600;

    private GUIStyle headerStyle;
    private GUIStyle textStyle;
    private GUIStyle boldTextStyle;
    private readonly GUILayoutOption buttonWidth = GUILayout.Width(100);
    private readonly SortedSet<providerInfo> providersSet = new SortedSet<providerInfo>(new ProviderInfoComparor());
    private providerInfo zBaseManagerProviderInfo;
    private PackageVersionModel packageVersion;

    public static void ShowZBaseDependenciesManager()
    {
        var win = GetWindowWithRect<ZBaseDependenciesManager>(new Rect(0, 0, Width, Height), true);
        win.titleContent = new GUIContent("Zitga Base Manager");
        win.Focus();
    }

    void Awake()
    {
        headerStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14,
            fixedHeight = 20,
            stretchWidth = true,
            fixedWidth = Width / 4 + 5,
            clipping = TextClipping.Overflow,
            alignment = TextAnchor.MiddleLeft
        };
        textStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft

        };
        boldTextStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold
        };

        //zBaseManagerProviderInfo = new providerInfo("Zitga Base Tool", "0.1.0", "0.1.0", providerInfo.Status.updated);

        //providerInfo info = new providerInfo();
        //info.displayProviderName = "package test";
        //info.currentStatues = providerInfo.Status.none;
        //providersSet.Add(info);
        ZBaseEditorCoroutines.StartEditorCoroutine(GetVersions());
        Repaint();
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            DrawToolHeader();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            DrawProviderItem(zBaseManagerProviderInfo);
            GUILayout.Space(5);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        GUILayout.Space(15);
        DrawPackageHeader();
        GUILayout.Space(15);

        foreach (var provider in providersSet)
        {
            DrawProviderItem(provider);
            GUILayout.Space(2);
        }

    }

    #region UI General
    private void DrawToolHeader()
    {
        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
        {
            EditorGUILayout.LabelField("Current Tool Version", new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                fixedHeight = 20,
                stretchWidth = true,
                fixedWidth = Width / 4,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(Width / 4 + 15, 0, 0, 0)
            });
            GUILayout.Space(85);
            EditorGUILayout.LabelField("Latest Tool Version", new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                fixedHeight = 20,
                stretchWidth = true,
                fixedWidth = Screen.width / 4,
                clipping = TextClipping.Overflow,
            });
        }
    }

    private void DrawPackageHeader()
    {
        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
        {
            EditorGUILayout.LabelField("Package", headerStyle);
            EditorGUILayout.LabelField("Current Package Version", headerStyle);
            EditorGUILayout.LabelField("Latest Package Version", headerStyle);
            GUILayout.Space(30);
            EditorGUILayout.LabelField("Action", headerStyle);
        }
    }

    void DrawProviderItem(providerInfo providerData)
    {
        if (!providerData.Equals(default(providerInfo)))
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
            {
                GUI.enabled = true;

                EditorGUILayout.LabelField(providerData.displayProviderName, textStyle);
                EditorGUILayout.LabelField(providerData.currentUnityVersion, textStyle);
                EditorGUILayout.LabelField(providerData.latestUnityVersion, textStyle);

                if (providerData.currentStatues == providerInfo.Status.none)
                {
                    bool btn = GUILayout.Button(new GUIContent
                    {
                        text = "Install",
                    }, buttonWidth);
                    if (btn)
                    {
                        GUI.enabled = true;
                        try
                        {
                            string url = "https://github.com/minhdt17/package-base-test.git?path=Packages/com.zitga.packagetest#0.2.0";
                            string name = "com.unity.2d.animation";
                            //ZBaseEditorCoroutines.StartEditorCoroutine(AddPackage(name, (result) =>
                            //{
                            //    if (result.Status == StatusCode.Success)
                            //        Debug.Log("Success!");
                            //}));

                            ZBaseEditorCoroutines.StartEditorCoroutine(SearchPackage(name, (result) =>
                            {
                                if (result.Status == StatusCode.Success)
                                    if (result.Result.Length > 0)
                                        Debug.Log(string.Format("Package {0}, lastest version {1}", result.Result[0].name, result.Result[0].version));
                            }));
                        }
                        catch (System.Exception)
                        {

                            throw;
                        }
                    }

                }
                else if (providerData.currentStatues == providerInfo.Status.installed)
                {
                    var btn = GUILayout.Button(new GUIContent
                    {
                        text = "Update",
                    }
                    , buttonWidth);
                    if (btn)
                    {
                        GUI.enabled = true;
                    }
                }
                else
                {
                    var btn = GUILayout.Button(new GUIContent
                    {
                        text = "Remove",
                    }, buttonWidth);
                    if (btn)
                    {
                        GUI.enabled = true;
                    }
                }
                GUILayout.Space(5);
                GUI.enabled = true;
            }
        }
    }
    #endregion

    #region Action
    private IEnumerator AddPackage(string urlOrPackageName, System.Action<AddRequest> callback)
    {
        var result = Client.Add(urlOrPackageName);

        while (!result.IsCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (result.Error != null)
        {
            Debug.LogError("[Error] Add Fail: " + result.Error.message);
            if (callback != null)
                callback(null);
        }
        else
        {
            if (callback != null)
                callback(result);
        }
    }

    private IEnumerator SearchPackage(string PackageName, System.Action<SearchRequest> callback)
    {
        var result = Client.Search(PackageName);

        while (!result.IsCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (result.Error != null)
        {
            Debug.LogError("[Error] Add Fail: " + result.Error.message);
            if (callback != null)
                callback(null);
        }
        else
        {
            if (callback != null)
                callback(result);
        }
    }
    #endregion

    #region Http
    private IEnumerator GetVersions()
    {
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(packVersionURL);
        var webRequest = unityWebRequest.SendWebRequest();

        while (!webRequest.isDone)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (unityWebRequest.isHttpError)
        {
            Debug.LogError("[Error] Load Fail: " + unityWebRequest.error);
        }

        if (!unityWebRequest.isHttpError && !unityWebRequest.isNetworkError)
        {
            string json = unityWebRequest.downloadHandler.text;
            Debug.Log("Data: " + json);
            providersSet.Clear();
            zBaseManagerProviderInfo = new providerInfo();
            Dictionary<string, object> dic = new Dictionary<string, object>();
            //            
            try
            {
                dic = Json.Deserialize(json) as Dictionary<string, object>;
                packageVersion = JsonUtility.FromJson<PackageVersionModel>(json);
            }

            catch (Exception e)
            {
                Debug.Log("Error getting response " + e.ToString());
            }
            //
            if (packageVersion != null)
            {
                Debug.Log("Dependencies count: " + packageVersion.dependencies);
            }
        }
    }
    #endregion

    public class providerInfo
    {
        public Status currentStatues;
        public string providerName;
        public string currentUnityVersion;
        public string latestUnityVersion;
        public string downloadURL;
        public string displayProviderName;
        public bool isNewProvider;
        public string fileName;
        public Dictionary<string, string> sdkVersionDic;

        public providerInfo()
        {
            isNewProvider = false;
            fileName = string.Empty;
            downloadURL = string.Empty;
            currentUnityVersion = "none";
            sdkVersionDic = new Dictionary<string, string>();
        }

        public providerInfo(string displayName, string currVer, string lastVer, Status currStatus)
        {
            providerName = displayProviderName = displayName;
            currentStatues = currStatus;
            currentUnityVersion = currVer;
            latestUnityVersion = lastVer;
            isNewProvider = false;
            fileName = string.Empty;
            downloadURL = string.Empty;
            sdkVersionDic = new Dictionary<string, string>();
        }

        public enum Status
        {
            installed = 1,
            none = 2,
            updated = 3
        }
    }

    internal class ProviderInfoComparor : IComparer<providerInfo>
    {
        public int Compare(providerInfo x, providerInfo y)
        {
            return x.providerName.CompareTo(y.providerName);
        }
    }

    public class PackageVersionModel
    {
        public string name;
        public string displayName;
        public string version;
        public PackageDependencies[] dependencies;
        public Dictionary<string, string> dictDependencies;

        public PackageVersionModel()
        {
            this.name = "";
            this.displayName = "";
            this.version = "";
            dictDependencies = new Dictionary<string, string>();
        }

        public class PackageDependencies
        {
            public string name;
            public string version;
            public PackageDependencies() { }
        }
    }
}

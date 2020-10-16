using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using ZBaseJsonHelper;

public class ZBaseDependenciesManager : EditorWindow
{
    private const string packVersionURL = "https://github.com/minhdt17/package-base-test/raw/main/Packages/com.zitga.packagetest/package.json";
    private const string packLockURL = "https://github.com/minhdt17/package-base-test/raw/main/Packages/packages-lock.json";
    private const string packLockLocalDir = "Packages/packages-lock.json";
    private const int Width = 760;
    private const int Height = 600;

    private GUIStyle headerStyle;
    private GUIStyle textStyle;
    private GUIStyle boldTextStyle;
    private readonly GUILayoutOption buttonWidth = GUILayout.Width(100);

    private readonly Dictionary<string, providerInfo> providersSet = new Dictionary<string, providerInfo>();
    private readonly Dictionary<string, providerInfo> providersLocal = new Dictionary<string, providerInfo>();
    private providerInfo zBaseManagerProviderServer;
    private providerInfo zBaseManagerProviderLocal;
    private ZBaseEditorCoroutines mEditorCoroutines;
    private bool isLoadVersionDone, isLoadPackLockDone;

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


        CheckVersion();

        Repaint();

    }

    private void CheckVersion()
    {
        isLoadVersionDone = false;
        isLoadPackLockDone = false;

        mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetVersions(packVersionURL, (result) => GetToolVersionInfoFromServer(result)));
        mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetVersions(packLockURL, (result) => GetVersionFromPackageLockServer(result)));
        mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetVersionFromPackageLockLocal());
    }

    private void CancelDownload()
    {
        if (mEditorCoroutines != null)
        {
            mEditorCoroutines.StopEditorCoroutine();
            mEditorCoroutines = null;
        }
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            DrawToolHeader();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            DrawProviderItem(zBaseManagerProviderLocal);
            GUILayout.Space(5);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        GUILayout.Space(15);
        DrawPackageHeader();
        GUILayout.Space(15);

        foreach (var provider in providersLocal)
        {
            DrawProviderItem(provider.Value);
            GUILayout.Space(2);
        }

        GUILayout.FlexibleSpace();
        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
        {
            GUILayout.Space(698);
            if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(30)))
            {
                CheckVersion();
            }
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
        if (providerData == null)
            return;

        if (!providerData.Equals(default(providerInfo)))
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
            {
                GUI.enabled = true;

                EditorGUILayout.LabelField(providerData.displayProviderName, textStyle);
                EditorGUILayout.LabelField(providerData.currentUnityVersion, textStyle);
                EditorGUILayout.LabelField(providerData.latestUnityVersion, textStyle);

                if (providerData.currentStatues == ZBaseEnum.Status.none)
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
                            ZBaseEditorCoroutines.StartEditorCoroutine(AddPackage(providerData.providerName, providerData.latestUnityVersion, providerData.source, (result) =>
                            {
                                if (result.Status == StatusCode.Success)
                                {
                                    Debug.Log(string.Format("***Download Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                    CheckVersion();
                                }
                            }));
                        }
                        catch (System.Exception)
                        {

                            throw;
                        }
                    }

                }
                else if (providerData.currentStatues == ZBaseEnum.Status.installed)
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
                    GUI.enabled = false;
                    GUILayout.Button(new GUIContent
                    {
                        text = "Updated",
                    }, buttonWidth);
                }
                GUILayout.Space(5);
                GUI.enabled = true;
            }
        }
    }
    #endregion

    #region Action
    private IEnumerator AddPackage(string urlOrPackageName, string version, ZBaseEnum.Source source, System.Action<AddRequest> callback)
    {
        AddRequest result = null;
        if (source == ZBaseEnum.Source.registry)
        {
            result = Client.Add(urlOrPackageName);
        }
        else if (source == ZBaseEnum.Source.git)
        {
            string urlDownload = urlOrPackageName + "#" + version;
            result = Client.Add(urlDownload);
        }

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
    private IEnumerator GetVersions(string url, System.Action<Dictionary<string, object>> callback)
    {
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(url);
        var webRequest = unityWebRequest.SendWebRequest();

        if (!unityWebRequest.isHttpError && !unityWebRequest.isNetworkError)
        {
            while (!webRequest.isDone)
            {
                yield return new WaitForSeconds(0.1f);
                if (EditorUtility.DisplayCancelableProgressBar("Check version", "", webRequest.progress))
                {
                    Debug.LogError("[Error] Check version fail: " + unityWebRequest.error);
                    CancelDownload();
                }
            }
            EditorUtility.ClearProgressBar();

            string json = unityWebRequest.downloadHandler.text;
            //Debug.Log("Data: " + json);

            Dictionary<string, object> dic = new Dictionary<string, object>();
            //            
            try
            {
                dic = Json.Deserialize(json) as Dictionary<string, object>;
                if (callback != null)
                    callback(dic);
            }

            catch (Exception e)
            {
                Debug.Log("Error parse data " + e.ToString());
            }

        }
        else
        {
            Debug.LogError("[Error] Load Fail: " + unityWebRequest.error);
        }
    }
    #endregion

    #region Parse Data
    // server
    private void GetToolVersionInfoFromServer(Dictionary<string, object> data)
    {
        zBaseManagerProviderServer = new providerInfo();
        foreach (var item in data)
        {
            try
            {
                if (item.Key.ToLower().Equals("name"))
                {
                    zBaseManagerProviderServer.providerName = item.Value as string;
                }
                else if (item.Key.ToLower().Equals("displayname"))
                {
                    zBaseManagerProviderServer.displayProviderName = item.Value as string;
                }
                else if (item.Key.ToLower().Equals("version"))
                {
                    zBaseManagerProviderServer.currentUnityVersion = zBaseManagerProviderServer.latestUnityVersion = item.Value as string;
                }

            }
            catch (Exception e)
            {
                Debug.Log("Error parse tool version info " + e.ToString());
            }
        }
        isLoadVersionDone = true;
        Debug.Log(string.Format("***Tool {0} on server, version {1}***", zBaseManagerProviderServer.displayProviderName, zBaseManagerProviderServer.latestUnityVersion));

    }

    private void GetVersionFromPackageLockServer(Dictionary<string, object> data)
    {
        providersSet.Clear();

        try
        {
            object dependencies;
            if (data.TryGetValue("dependencies", out dependencies))
            {
                if (dependencies != null)
                {
                    Dictionary<string, object> listPackages = dependencies as Dictionary<string, object>;
                    foreach (var item in ZBasePackageIdConfig.listPackages)
                    {
                        providerInfo info = new providerInfo();
                    }

                    foreach (var item in dependencies as Dictionary<string, object>)
                    {
                        providerInfo info = new providerInfo();
                        if (ZBasePackageIdConfig.listPackages.ContainsKey(item.Key))
                        {
                            if (info.GetFromJson(item.Key, item.Value as Dictionary<string, object>))
                            {
                                if (item.Key.ToLower().Equals(ZBasePackageIdConfig.namePackageManager))
                                {
                                    continue;
                                }
                                else
                                {
                                    Debug.Log(string.Format("***Package {0} on server, version {1}***", info.displayProviderName, info.latestUnityVersion));
                                    providersSet.Add(info.providerName, info);
                                }
                            }
                        }
                    }
                }
            }

            isLoadPackLockDone = true;
        }
        catch (Exception e)
        {
            Debug.Log("Error Get Version From Package Lock Server: " + e.Message);
        }
    }

    // local
    private IEnumerator GetVersionFromPackageLockLocal()
    {

        while (!IsLoadDataServerDone())
        {
            yield return new WaitForSeconds(0.1f);
        }

        Dictionary<string, object> dic = new Dictionary<string, object>();
        providersLocal.Clear();

        try
        {
            string fileContent = File.ReadAllText(packLockLocalDir);
            dic = Json.Deserialize(fileContent) as Dictionary<string, object>;
            object dependencies;
            if (dic.TryGetValue("dependencies", out dependencies))
            {
                if (dependencies != null)
                {
                    Dictionary<string, object> listPackages = dependencies as Dictionary<string, object>;

                    foreach (var item in dependencies as Dictionary<string, object>)
                    {
                        providerInfo info = new providerInfo();
                        if (ZBasePackageIdConfig.listPackages.ContainsKey(item.Key))
                        {
                            if (info.GetFromJson(item.Key, item.Value as Dictionary<string, object>))
                            {
                                if (item.Key.ToLower().Equals(ZBasePackageIdConfig.namePackageManager))
                                {
                                    zBaseManagerProviderLocal = info;
                                    Debug.Log(string.Format(">>>Tool {0} on local, version {1}<<<", zBaseManagerProviderLocal.displayProviderName, zBaseManagerProviderLocal.latestUnityVersion));
                                }
                                else
                                {
                                    Debug.Log(string.Format(">>>Package {0} on local, version {1}<<<", info.displayProviderName, info.latestUnityVersion));
                                    providersLocal.Add(info.providerName, info);
                                }
                            }
                        }
                    }

                    CompareVersion();

                    if (providersLocal.Count != ZBasePackageIdConfig.listPackages.Count)
                    {
                        bool isCheck = false;

                        foreach (var item in ZBasePackageIdConfig.listPackages)
                        {
                            if (item.Key == ZBasePackageIdConfig.namePackageManager)
                                continue;

                            isCheck = false;
                            foreach (var provider in providersLocal)
                            {
                                if (provider.Key == item.Key)
                                {
                                    isCheck = true;
                                    break;
                                }
                            }

                            if (!isCheck)
                            {
                                providerInfo info = providersSet[item.Key].ShallowCopy();
                                info.currentStatues = ZBaseEnum.Status.none;
                                info.currentUnityVersion = "none";
                                providersLocal.Add(info.providerName, info);
                                Debug.Log(string.Format(">>>Package {0} not install<<<", info.displayProviderName));
                            }

                        }
                    }

                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error Get Version From Package Lock Local: " + e.Message);
        }
    }

    private bool IsLoadDataServerDone()
    {
        if (isLoadVersionDone && isLoadPackLockDone)
            return true;
        else
            return false;
    }

    #endregion

    public class providerInfo
    {


        public ZBaseEnum.Status currentStatues;
        public string providerName;
        public string displayProviderName;
        public string currentUnityVersion;
        public string latestUnityVersion;
        public string downloadURL;
        public ZBaseEnum.Source source;

        public providerInfo ShallowCopy()
        {
            return (providerInfo)this.MemberwiseClone();
        }


        public providerInfo()
        {
            currentStatues = ZBaseEnum.Status.none;
            providerName = displayProviderName = string.Empty;
            source = ZBaseEnum.Source.registry;
            downloadURL = string.Empty;
            currentUnityVersion = "none";
        }

        public providerInfo(string providerName, string displayName, string currVer, string lastVer, ZBaseEnum.Status currStatus, ZBaseEnum.Source source, string urlDownload = "")
        {
            this.providerName = providerName;
            this.displayProviderName = displayName;
            this.currentStatues = currStatus;
            this.currentUnityVersion = currVer;
            this.latestUnityVersion = lastVer;
            this.source = source;
            this.downloadURL = urlDownload;
        }



        public bool GetFromJson(string name, Dictionary<string, object> dic)
        {
            providerName = name;
            object obj;

            //source
            dic.TryGetValue("source", out obj);
            if (obj != null)
            {
                ZBaseEnum.Source result;
                if (Enum.TryParse(obj as string, out result))
                {
                    this.source = result;
                }
            }
            //display name
            if (ZBasePackageIdConfig.listPackages.ContainsKey(name))
                this.displayProviderName = ZBasePackageIdConfig.listPackages[name];
            //version, url
            dic.TryGetValue("version", out obj);
            if (obj != null)
            {
                if (this.source == ZBaseEnum.Source.registry)
                {
                    this.currentUnityVersion = this.latestUnityVersion = obj as string;
                }
                else if (this.source == ZBaseEnum.Source.git)
                {
                    string objString = obj as string;
                    string[] arrString = objString.Split('#'); // url = urlGit + # + version
                    if (arrString.Length >= 2)
                    {
                        this.downloadURL = arrString[0];
                        this.currentUnityVersion = this.latestUnityVersion = arrString[1];
                    }
                }
            }

            return true;
        }
    }

    private void CompareVersion()
    {
        // Tool manager
        if (zBaseManagerProviderLocal.source == ZBaseEnum.Source.embedded)
        {
            zBaseManagerProviderLocal.currentUnityVersion = zBaseManagerProviderLocal.latestUnityVersion = zBaseManagerProviderServer.latestUnityVersion;
            zBaseManagerProviderLocal.currentStatues = ZBaseEnum.Status.updated;
        }
        else
        {
            if (isNewerVersion(zBaseManagerProviderLocal.currentUnityVersion, zBaseManagerProviderServer.latestUnityVersion))
            {
                zBaseManagerProviderLocal.currentStatues = ZBaseEnum.Status.installed;
                zBaseManagerProviderLocal.latestUnityVersion = zBaseManagerProviderServer.latestUnityVersion;
            }
            else
            {
                zBaseManagerProviderLocal.currentStatues = ZBaseEnum.Status.updated;
            }
        }

        // Package
        foreach (var item in providersLocal)
        {
            var providerServer = providersSet[item.Key];
            if (isNewerVersion(item.Value.currentUnityVersion, providerServer.latestUnityVersion))
            {
                item.Value.currentStatues = ZBaseEnum.Status.installed;
                item.Value.latestUnityVersion = providerServer.latestUnityVersion;
            }
            else
            {
                item.Value.currentStatues = ZBaseEnum.Status.updated;
            }
        }
    }

    private static bool isNewerVersion(string current, string latest)
    {
        bool isNewer = false;
        try
        {
            int[] currentVersion = Array.ConvertAll(current.Split('.'), int.Parse);
            int[] remoteVersion = Array.ConvertAll(latest.Split('.'), int.Parse);
            int remoteBuild = 0;
            int curBuild = 0;
            if (currentVersion.Length > 3)
            {
                curBuild = currentVersion[3];
            }
            if (remoteVersion.Length > 3)
            {
                remoteBuild = remoteVersion[3];

            }
            System.Version cur = new System.Version(currentVersion[0], currentVersion[1], currentVersion[2], curBuild);
            System.Version remote = new System.Version(remoteVersion[0], remoteVersion[1], remoteVersion[2], remoteBuild);
            isNewer = cur < remote;
        }
        catch (Exception ex)
        {

        }
        return isNewer;

    }

}

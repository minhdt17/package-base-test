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
    private const int LOAD_DATA_COMPLETE = 3;
    private const string installURL = "https://github.com/minhdt17/{0}.git?path=Packages/{1}";
    private const string suffixesVersionGitURL = "#{0}";
    private const string latestTagURL = "https://api.github.com/repos/minhdt17/{0}/releases/latest";
    private const string packLockURL = "https://github.com/minhdt17/{0}/raw/main/Packages/packages-lock.json";
    private const string packVersionURL = "https://github.com/minhdt17/{0}/raw/main/Packages/{1}/package.json";
    private const string packLockLocalDir = "Packages/packages-lock.json";
    private const string packVersionLocalDir = "Packages/{0}/package.json";
    private const string packCacheLocalDir = "Library/PackageCache/{0}@{1}/package.json";
    private const int Width = 760;
    private const int Height = 600;

    private GUIStyle headerStyle;
    private GUIStyle textStyle;
    private GUIStyle boldTextStyle;
    private readonly GUILayoutOption buttonWidth = GUILayout.Width(60);

    private readonly Dictionary<string, ProviderInfo> providersSet = new Dictionary<string, ProviderInfo>();
    private readonly Dictionary<string, ProviderInfo> providersLocal = new Dictionary<string, ProviderInfo>();
    //private providerInfo zBaseManagerProviderLocal;
    private ZBaseEditorCoroutines mEditorCoroutines;
    private int progressLoadData = 0;
    private bool isProcessing;
    private bool canRefresh;
    private string latest_tag = string.Empty;

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
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };


        CheckVersion();

    }

    void OnDestroy()
    {
        CancelDownload();
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            DrawToolHeader();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            DrawProviderManager();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        GUILayout.Space(15);
        DrawPackageHeader();
        GUILayout.Space(15);

        foreach (var provider in providersLocal)
        {
            if (provider.Value.providerName == ZBasePackageIdConfig.namePackageManager)
                continue;

            DrawProviderItem(provider.Value);
            GUILayout.Space(2);
        }

        GUILayout.FlexibleSpace();
        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
        {
            GUILayout.Space(698);
            if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(30)) && !isProcessing)
            {
                Refresh();
            }
        }
    }

    void OnInspectorUpdate()
    {
        if (canRefresh)
        {
            Debug.Log("**********Refresh*************");
            Refresh();
        }
    }

    #region Funnction
    private void CheckVersion()
    {
        latest_tag = string.Empty;
        progressLoadData = 0;

        GetLatestTagRelease();
        mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetPackageLockServer());
        mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetVersionFromPackageLockLocal());
    }

    private void CancelDownload()
    {
        isProcessing = false;

        if (mEditorCoroutines != null)
        {
            mEditorCoroutines.StopEditorCoroutine();
            mEditorCoroutines = null;
        }
    }

    private void Refresh()
    {
        canRefresh = false;
        CancelDownload();
        CheckVersion();
    }
    #endregion


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
                padding = new RectOffset(Width / 4 + 15, 0, 0, 0),
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
            GUILayout.Space(40);
            EditorGUILayout.LabelField("Action", headerStyle);
        }
    }

    void DrawProviderManager()
    {
        if (providersLocal.ContainsKey(ZBasePackageIdConfig.namePackageManager))
        {
            ProviderInfo providerData = providersLocal[ZBasePackageIdConfig.namePackageManager];
            DrawProviderItem(providerData);
        }
    }

    void DrawProviderItem(ProviderInfo providerData)
    {
        if (providerData == null)
            return;

        if (!providerData.Equals(default(ProviderInfo)))
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
            {
                GUI.enabled = true;

                EditorGUILayout.LabelField(providerData.displayProviderName, textStyle);
                EditorGUILayout.LabelField(providerData.currentUnityVersion, textStyle);
                EditorGUILayout.LabelField(providerData.latestUnityVersion, textStyle);

                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                {
                    if (providerData.currentStatues == ZBaseEnum.Status.none)
                    {
                        bool btn = GUILayout.Button(new GUIContent
                        {
                            text = "Install",
                        }, buttonWidth);
                        if (btn && !isProcessing)
                        {
                            GUI.enabled = true;
                            try
                            {
                                Debug.LogWarning(">>>>>>>>> Install Click! <<<<<<<<<<");
                                ZBaseEditorCoroutines.StartEditorCoroutine(AddPackage(providerData, (result) =>
                                {
                                    if (result.Status == StatusCode.Success)
                                    {
                                        Debug.Log(string.Format("***Install Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                        canRefresh = true;
                                    }
                                }));
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError("Error " + e.Message);
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
                        if (btn && !isProcessing)
                        {
                            GUI.enabled = true;
                            try
                            {
                                Debug.LogWarning(">>>>>>>>> Update Click! <<<<<<<<<<");
                                ZBaseEditorCoroutines.StartEditorCoroutine(AddPackage(providerData, (result) =>
                                {
                                    if (result.Status == StatusCode.Success)
                                    {
                                        Debug.Log(string.Format("***Update Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                        canRefresh = true;
                                    }
                                }));
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError("Error " + e.Message);
                            }
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

                    if (providerData.currentStatues != ZBaseEnum.Status.none && providerData.providerName != ZBasePackageIdConfig.namePackageManager)
                    {
                        GUI.enabled = true;
                        var btn = GUILayout.Button(new GUIContent
                        {
                            text = "Remove",
                        }
                        , buttonWidth);
                        if (btn && !isProcessing)
                        {
                            GUI.enabled = true;
                            try
                            {
                                Debug.LogWarning(">>>>>>>>> Remove Click! <<<<<<<<<<");
                                ZBaseEditorCoroutines.StartEditorCoroutine(RemovePackage(providerData.providerName, (result) =>
                                {
                                    if (result.Status == StatusCode.Success)
                                    {
                                        Debug.Log(string.Format("***Remove Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                        canRefresh = true;
                                    }
                                }));
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError("Error " + e.Message);
                            }
                        }
                    }
                }

                GUILayout.Space(5);
                GUI.enabled = true;
            }
        }
    }
    #endregion

    #region Action
    private IEnumerator AddPackage(ProviderInfo providerInfo, System.Action<AddRequest> callback)
    {
        AddRequest result = null;
        string urlDownload = "";
        if (providerInfo.source == ZBaseEnum.Source.git)
            urlDownload = providerInfo.downloadURL + string.Format(suffixesVersionGitURL, providerInfo.latestUnityVersion);
        else
            urlDownload = string.Format(installURL, ZBasePackageIdConfig.REPO, providerInfo.providerName);

        result = Client.Add(urlDownload);

        while (!result.IsCompleted)
        {
            isProcessing = true;
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
            isProcessing = true;
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

    private IEnumerator RemovePackage(string PackageName, System.Action<RemoveRequest> callback)
    {
        var result = Client.Remove(PackageName);

        while (!result.IsCompleted)
        {
            isProcessing = true;
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
    private void GetLatestTagRelease()
    {
        string urlLatestRelease = string.Format(latestTagURL, ZBasePackageIdConfig.REPO);
        mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetRequest(urlLatestRelease, (result) => GetLatestTag(result)));
    }

    private IEnumerator GetPackageLockServer()
    {
        while (!string.IsNullOrEmpty(latest_tag))
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (latest_tag == "none")
            yield return null;

        string urlPackageLock = string.Format(packLockURL, ZBasePackageIdConfig.REPO);
        mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetRequest(urlPackageLock, (result) => GetDataFromPackageLockServer(result)));
    }

    private IEnumerator GetVersionForEmbeddedPack()
    {
        int numbQuest = 0;
        foreach (var item in providersSet)
        {
            ProviderInfo info = item.Value;
            if (info.source == ZBaseEnum.Source.embedded)
            {
                numbQuest++;
                GetPackageFromServer(info.providerName, delegate (Dictionary<string, object> result)
                {
                    info.GetVersionInfoFromServer(result);
                    numbQuest--;
                });
            }
        }

        while (numbQuest > 0)
        {
            yield return new WaitForSeconds(0.1f);
        }

        progressLoadData++;
    }

    private void GetPackageFromServer(string packageName, System.Action<Dictionary<string, object>> callback)
    {
        string urlPackage = string.Format(packVersionURL, ZBasePackageIdConfig.REPO, packageName);
        mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetRequest(urlPackage, (result) => callback(result)));
    }

    private IEnumerator GetRequest(string url, System.Action<Dictionary<string, object>> callback)
    {
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(url);
        var webRequest = unityWebRequest.SendWebRequest();

        if (!unityWebRequest.isHttpError && !unityWebRequest.isNetworkError)
        {
            Debug.Log("[Get] URL: " + url);
            while (!webRequest.isDone)
            {
                yield return new WaitForSeconds(0.1f);
                if (EditorUtility.DisplayCancelableProgressBar("Downloading...", "", webRequest.progress))
                {
                    Debug.LogError(string.Format("[Get] URL: {0}\n{1}", url, unityWebRequest.error));
                    CancelDownload();
                }
            }
            EditorUtility.ClearProgressBar();

            string json = unityWebRequest.downloadHandler.text;
            Debug.Log("Data: " + json);

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
                Debug.LogError("[Parse Data] Error: " + e.ToString());
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
    private void GetLatestTag(Dictionary<string, object> data)
    {
        string tagName = string.Empty;

        foreach (var item in data)
        {
            try
            {
                if (item.Key.ToLower().Equals("message"))
                {
                    this.latest_tag = "none";
                    Debug.LogError("Error get latest release: " + item.Value);
                    return;
                }

                if (item.Key.ToLower().Equals("tag_name"))
                {
                    tagName = item.Value as string;
                    if (!string.IsNullOrEmpty(tagName))
                        this.latest_tag = tagName;
                    else
                        this.latest_tag = "none";
                }
            }
            catch (Exception e)
            {
                this.latest_tag = "none";
                Debug.LogError("Error get latest release: " + e.ToString());
                throw;
            }
        }

        progressLoadData++;
        Debug.Log("Latest tag is " + this.latest_tag);
    }

    //private void GetToolVersionInfoFromServer(Dictionary<string, object> data)
    //{
    //    zBaseManagerProviderServer = new providerInfo();
    //    foreach (var item in data)
    //    {
    //        try
    //        {
    //            if (item.Key.ToLower().Equals("name"))
    //            {
    //                zBaseManagerProviderServer.providerName = item.Value as string;
    //            }
    //            else if (item.Key.ToLower().Equals("displayname"))
    //            {
    //                zBaseManagerProviderServer.displayProviderName = item.Value as string;
    //            }
    //            else if (item.Key.ToLower().Equals("version"))
    //            {
    //                zBaseManagerProviderServer.currentUnityVersion = zBaseManagerProviderServer.latestUnityVersion = item.Value as string;
    //            }

    //        }
    //        catch (Exception e)
    //        {
    //            Debug.Log("Error parse tool version info " + e.ToString());
    //        }
    //    }

    //    progressLoadData++;
    //    Debug.Log(string.Format("***Tool {0} on server, version {1}***", zBaseManagerProviderServer.displayProviderName, zBaseManagerProviderServer.latestUnityVersion));

    //}

    private void GetDataFromPackageLockServer(Dictionary<string, object> data)
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

                    foreach (var item in dependencies as Dictionary<string, object>)
                    {
                        ProviderInfo info = new ProviderInfo();
                        if (ZBasePackageIdConfig.listPackages.ContainsKey(item.Key))
                        {
                            if (info.GetFromJson(item.Key, item.Value as Dictionary<string, object>))
                            {
                                providersSet.Add(info.providerName, info);
                                if (info.currentUnityVersion != "none")
                                    Debug.Log(string.Format("***Package {0} on server, version {1}***", info.displayProviderName, info.latestUnityVersion));
                            }
                        }
                    }
                }
            }

            progressLoadData++;

            ZBaseEditorCoroutines.StartEditorCoroutine(GetVersionForEmbeddedPack());
        }
        catch (Exception e)
        {
            Debug.LogError("Error Get Version From Package Lock Server: " + e.Message);
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
                        ProviderInfo info = new ProviderInfo();
                        if (ZBasePackageIdConfig.listPackages.ContainsKey(item.Key))
                        {
                            if (info.GetFromJson(item.Key, item.Value as Dictionary<string, object>))
                            {
                                //if (item.Key.ToLower().Equals(ZBasePackageIdConfig.namePackageManager))
                                //{
                                //    zBaseManagerProviderLocal = info;
                                //    if (zBaseManagerProviderLocal.currentUnityVersion == "none")
                                //        zBaseManagerProviderLocal.currentUnityVersion = zBaseManagerProviderLocal.latestUnityVersion = providersSet[ZBasePackageIdConfig.namePackageManager].latestUnityVersion;

                                //    Debug.Log(string.Format(">>>Tool {0} on local, version {1}<<<", zBaseManagerProviderLocal.displayProviderName, zBaseManagerProviderLocal.currentUnityVersion));
                                //}
                                //else
                                //{
                                providersLocal.Add(info.providerName, info);
                                if (info.currentUnityVersion != "none")
                                    Debug.Log(string.Format(">>>Package {0} on local, version {1}<<<", info.displayProviderName, info.currentUnityVersion));
                                //}
                            }
                        }
                    }

                    foreach (var item in providersLocal)
                    {
                        ProviderInfo info = item.Value;
                        if (info.source == ZBaseEnum.Source.embedded && info.currentUnityVersion == "none")
                        {
                            LoadPackageFromLocal(info.providerName, info.GetVersionInfoFromLocal);
                        }
                        else if (info.source == ZBaseEnum.Source.git && info.currentUnityVersion == "none" && !string.IsNullOrEmpty(info.hash))
                        {
                            LoadPackageCacheFromLocal(info.providerName, info.hash, info.GetVersionInfoFromLocal);
                        }
                    }

                    CompareVersion();

                    //check package not install
                    if (providersLocal.Count != ZBasePackageIdConfig.listPackages.Count) //skip item package manager
                    {
                        foreach (var item in ZBasePackageIdConfig.listPackages)
                        {
                            if (providersLocal.ContainsKey(item.Key))
                                continue;

                            if (!providersSet.ContainsKey(item.Key))
                                continue;

                            ProviderInfo info = providersSet[item.Key].ShallowCopy();
                            info.currentStatues = ZBaseEnum.Status.none;
                            info.currentUnityVersion = "none";
                            providersLocal.Add(info.providerName, info);
                            Debug.Log(string.Format(">>>Package {0} not install<<<", info.displayProviderName));
                        }
                    }

                }
            }

            Repaint();
        }
        catch (Exception e)
        {
            Debug.Log("Error Get Version From Package Lock Local: " + e.Message);
        }
    }

    private void LoadPackageFromLocal(string namePackage, System.Action<Dictionary<string, object>> callback)
    {
        try
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            string path = string.Format(packVersionLocalDir, namePackage);
            string fileContent = File.ReadAllText(path);
            dic = Json.Deserialize(fileContent) as Dictionary<string, object>;

            if (dic.Count > 0)
            {
                if (callback != null)
                    callback(dic);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error Load Package From Local: " + e.Message);
        }
    }

    private void LoadPackageCacheFromLocal(string namePackage, string hash, System.Action<Dictionary<string, object>> callback)
    {
        try
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            string path = string.Format(packCacheLocalDir, namePackage, hash);
            string fileContent = File.ReadAllText(path);
            dic = Json.Deserialize(fileContent) as Dictionary<string, object>;

            if (dic.Count > 0)
            {
                if (callback != null)
                    callback(dic);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error Load Package Cache From Local: " + e.Message);
        }
    }

    private bool IsLoadDataServerDone()
    {
        if (progressLoadData >= LOAD_DATA_COMPLETE)
            return true;
        else
            return false;
    }

    #endregion

    #region Utility
    private void CompareVersion()
    {
        // Tool manager
        //if (zBaseManagerProviderLocal.source == ZBaseEnum.Source.embedded)
        //{
        //    zBaseManagerProviderLocal.currentStatues = ZBaseEnum.Status.updated;
        //}
        //else
        //{
        //    if (isNewerVersion(zBaseManagerProviderLocal.currentUnityVersion, providersSet[ZBasePackageIdConfig.namePackageManager].latestUnityVersion))
        //    {
        //        zBaseManagerProviderLocal.currentStatues = ZBaseEnum.Status.installed;
        //        zBaseManagerProviderLocal.latestUnityVersion = providersSet[ZBasePackageIdConfig.namePackageManager].latestUnityVersion;
        //    }
        //    else
        //    {
        //        zBaseManagerProviderLocal.currentStatues = ZBaseEnum.Status.updated;
        //    }
        //}

        // Package
        foreach (var item in providersLocal)
        {
            var providerServer = providersSet[item.Key];
            if (isNewerVersion(item.Value.currentUnityVersion, providerServer.latestUnityVersion))
            {
                item.Value.currentStatues = ZBaseEnum.Status.installed;
            }
            else
            {
                item.Value.currentStatues = ZBaseEnum.Status.updated;
            }
            item.Value.latestUnityVersion = providerServer.latestUnityVersion;
        }
    }

    private bool isNewerVersion(string current, string latest)
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
        catch (Exception e)
        {
            Debug.LogError("Error " + e.Message);
        }
        return isNewer;
    }
    #endregion
}

public class ProviderInfo
{


    public ZBaseEnum.Status currentStatues;
    public string providerName;
    public string displayProviderName;
    public string currentUnityVersion;
    public string latestUnityVersion;
    public string downloadURL;
    public string hash;
    public ZBaseEnum.Source source;

    public ProviderInfo ShallowCopy()
    {
        return (ProviderInfo)this.MemberwiseClone();
    }


    public ProviderInfo()
    {
        currentStatues = ZBaseEnum.Status.none;
        providerName = displayProviderName = string.Empty;
        source = ZBaseEnum.Source.registry;
        downloadURL = string.Empty;
        currentUnityVersion = "none";
    }

    public ProviderInfo(string providerName, string displayName, string currVer, string lastVer, ZBaseEnum.Status currStatus, ZBaseEnum.Source source, string urlDownload = "")
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
        //hash
        dic.TryGetValue("hash", out obj);
        if (obj != null)
        {

            this.hash = obj as string;
            this.hash = this.hash.Remove(10);
        }

        return true;
    }

    public void GetVersionInfoFromServer(Dictionary<string, object> data)
    {
        foreach (var item in data)
        {
            try
            {
                if (item.Key.ToLower().Equals("version"))
                {
                    this.currentUnityVersion = this.latestUnityVersion = item.Value as string;
                }

            }
            catch (Exception e)
            {
                Debug.Log("Error parse tool version info " + e.ToString());
            }
        }

        Debug.Log(string.Format("***Pack {0} on server, version {1}***", this.displayProviderName, this.latestUnityVersion));
    }

    public void GetVersionInfoFromLocal(Dictionary<string, object> data)
    {
        foreach (var item in data)
        {
            try
            {
                if (item.Key.ToLower().Equals("version"))
                {
                    this.currentUnityVersion = item.Value as string;
                }

            }
            catch (Exception e)
            {
                Debug.Log("Error parse tool version info " + e.ToString());
            }
        }

        Debug.Log(string.Format("***Pack {0} on local, version {1}***", this.displayProviderName, this.currentUnityVersion));
    }
}
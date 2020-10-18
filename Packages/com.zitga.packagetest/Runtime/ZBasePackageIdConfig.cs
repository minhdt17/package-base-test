using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZBasePackageIdConfig
{
    public static readonly string REPO = "package-base-test";
    public static readonly string namePackageManager = "com.zitga.packagetest";
    public static readonly Dictionary<string, string> listPackages = new Dictionary<string, string>() {
        { "com.zitga.packagetest", "Package Manager" },
        { "com.cysharp.unitask", "Unitask" },
        { "com.zitga.packa", "Package A" },
        { "com.zitga.packb", "Package B" },
        { "com.zitga.packc", "Package C" },        
    };
}

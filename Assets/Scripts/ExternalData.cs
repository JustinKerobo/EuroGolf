using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Loads settings from ExternalData/ExternalData.txt and saves them.
/// </summary>
public static class ExternalData
{
    public static Dictionary<string, string> ExtData;
    public static bool Loading;

    /// <summary>
    ///     Loads the data.
    /// </summary>
    public static IEnumerator LoadData()
    {
        Loading = true;
        ExtData = new Dictionary<string, string>();

        const string fileName = "ExternalData/ExternalData.txt";

        string pathName;

        if (!Application.isWebPlayer)
            pathName =  "file://" + Application.dataPath + "/../" + fileName;
        else
            pathName = Application.dataPath + "/" + fileName;
   
        var www = new WWW(pathName);
        yield return www;

        var iniData = www.text;
        var aStrings = iniData.Split('\n');

        foreach (var line in aStrings)
        {
            var aKeyValue = line.Split('=');
            ExtData.Add(aKeyValue[0], aKeyValue[1].Trim());
        }

        Loading = false;
    }
}
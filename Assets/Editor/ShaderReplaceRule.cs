using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEditor;

[Serializable]
public class ShaderPropInfo
{
    public string name;
    public ShaderPropertyType type;
    public string desc;
    public bool visible = true;
}

public class ShaderReplaceRule : ScriptableObject
{
    public Shader resShader;
    public Shader destShader;
    public List<ShaderPropInfo> resPropertyInfoList = new List<ShaderPropInfo>();
    public List<ShaderPropInfo> destPropertyInfoList = new List<ShaderPropInfo>();
    public List<int> propertyMapping = new List<int>();

    public void SetShader(Shader shader,bool isRes)
    {
        EditorUtility.SetDirty(this);
        if(isRes)
            resShader = shader;
        else
            destShader = shader;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void SetMapping(int resIndex,int destIndex)
    {
        EditorUtility.SetDirty(this);
        propertyMapping[resIndex] = destIndex;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void ResetMapping()
    {
        EditorUtility.SetDirty(this);
        propertyMapping.Clear();
        if(resPropertyInfoList != null && resPropertyInfoList.Count > 0)
        {
            foreach (var pi in resPropertyInfoList)
            {
                propertyMapping.Add(-1);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
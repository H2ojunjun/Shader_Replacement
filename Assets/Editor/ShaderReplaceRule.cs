using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Serialization;

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
    [SerializeField]
    private Shader _resShader;

    [SerializeField]
    private Shader _destShader;

    public List<ShaderPropInfo> resPropertyInfoList = new List<ShaderPropInfo>();
    public List<ShaderPropInfo> destPropertyInfoList = new List<ShaderPropInfo>();
    public List<int> propertyMapping = new List<int>();

    public Shader ResShader => _resShader;

    public Shader DestShader => _destShader;

    public void SetShader(Shader resShader, Shader destShader)
    {
        if(resShader == null || destShader == null)
        {
            Debug.LogError("at least one shader is null");
            return;
        }
        EditorUtility.SetDirty(this);
        _resShader = resShader;
        _destShader = destShader;
        ResetCachePropInfo();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void SetMapping(int resIndex, int destIndex)
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
        if (resPropertyInfoList != null && resPropertyInfoList.Count > 0)
        {
            foreach (var pi in resPropertyInfoList)
            {
                propertyMapping.Add(-1);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void ResetCachePropInfo()
    {
        // List<ShaderPropInfo> shaderInfoList = null;
        // Shader shader = null;
        //List<Rect> ports = null;
        // if (isRes)
        // {
        //     shaderInfoList = resPropertyInfoList;
        //     shader = _resShader;
        //     //ports = _resPorts;
        // }
        // else
        // {
        //     shaderInfoList = destPropertyInfoList;
        //     shader = _destShader;
        //     //ports = _destPorts;
        // }

        resPropertyInfoList.Clear();
        destPropertyInfoList.Clear();
        //ports.Clear();

        int count = _resShader.GetPropertyCount();
        for (int i = 0; i < count; i++)
        {
            ShaderPropInfo pi = new ShaderPropInfo
            {
                name = _resShader.GetPropertyName(i),
                desc = _resShader.GetPropertyDescription(i),
                type = _resShader.GetPropertyType(i)
            };
            resPropertyInfoList.Add(pi);
        }
        
        count = _destShader.GetPropertyCount();
        for (int i = 0; i < count; i++)
        {
            ShaderPropInfo pi = new ShaderPropInfo
            {
                name = _destShader.GetPropertyName(i),
                desc = _destShader.GetPropertyDescription(i),
                type = _destShader.GetPropertyType(i)
            };
            destPropertyInfoList.Add(pi);
        }
        

        ResetMapping();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;

public class ShaderReplacementWindow : EditorWindow
{
    public enum EShowType
    {
        Name,
        Description
    }

    [MenuItem("Tools/ShaderReplacement")]
    public static void ShowWindow()
    {
        var window = (ShaderReplacementWindow)GetWindow<ShaderReplacementWindow>("Shader Replacement", true);
        window.Show();
    }

    private string ruleFolderPath;

    private Shader _resShader;
    private Shader _destShader;
    private ShaderReplaceRule _rule;
    private EShowType _replacementType = EShowType.Description;
    private Vector2 _scrollRoot = Vector2.zero;
    private Vector2 _matScrollRoot = Vector2.zero;
    private float _propHeight = 30;
    private float _propInterval = 3;
    private List<Rect> _resPorts = new List<Rect>();
    private List<Rect> _destPorts = new List<Rect>();
    private Rect _ruleRect = new Rect();
    private Rect _scrollArea;
    private int _draggingIndex = -1;
    private bool _showType = true;
    private bool _isOnlyShowConnect = false;
    private List<Material> _replacedMat = new List<Material>();
    private string _resSearchStr;

    private string _destSearchStr;
    //private Texture _dot;
    // private Texture dot
    // {
    //     get
    //     {
    //         if (_dot == null)
    //         {
    //             _dot = Resources.Load<Texture>("shaderReplace_dot");
    //         }
    //         return _dot;
    //     }
    // }

    private ShaderReplaceRule Rule
    {
        get => _rule;
        set
        {
            if (_rule != value)
            {
                _rule = value;
                if (_rule != null)
                {
                    _resShader = _rule.ResShader;
                    _destShader = _rule.DestShader;
                }
                else
                {
                    _resShader = null;
                    _destShader = null;
                }

                _resPorts.Clear();
                _destPorts.Clear();
            }
        }
    }

    private string GetShaderName(Shader shader)
    {
        return shader.name.Replace("/", "_");
    }

    private void DrawBaseInfo()
    {
        GUI.enabled = true;
        if (Rule != null)
        {
            GUI.enabled = false;
        }

        _resShader = (Shader)EditorGUILayout.ObjectField("源shader", _resShader, typeof(Shader), false, GUILayout.Width(500));
        _destShader = (Shader)EditorGUILayout.ObjectField("目标shader", _destShader, typeof(Shader), false, GUILayout.Width(500));
        GUI.enabled = true;
        Rule = (ShaderReplaceRule)EditorGUILayout.ObjectField("替换规则", Rule, typeof(ShaderReplaceRule), false, GUILayout.Width(500), GUILayout.Height(20));
        if (Rule == null)
        {
            if (_resShader != null && _destShader != null && GUILayout.Button("创建规则",GUILayout.Width(500), GUILayout.Height(20)))
            {
                string assetName = $"replace_shader_{GetShaderName(_resShader)}_to_{GetShaderName(_destShader)}";
                var userSelectPath = EditorUtility.SaveFilePanelInProject("Save shaderReplacementRule:", assetName, "asset", "");
                if (string.IsNullOrEmpty(userSelectPath))
                {
                    Debug.LogError("未选择路径");
                    return;
                }

                if (!userSelectPath.Contains("Editor"))
                {
                    Debug.LogError("路径中必须包含Editor");
                    return;
                }

                var savedRule = AssetDatabase.LoadAssetAtPath<ShaderReplaceRule>(userSelectPath);
                if (savedRule != null)
                {
                    AssetDatabase.DeleteAsset(userSelectPath);
                    AssetDatabase.Refresh();
                }
                var rule = ScriptableObject.CreateInstance<ShaderReplaceRule>();
                AssetDatabase.CreateAsset(rule, userSelectPath);
                rule.SetShader(_resShader, _destShader);
                Rule = rule;
            }
        }
        else
        {
            if (GUILayout.Button("重置",GUILayout.Width(500), GUILayout.Height(20)))
            {
                Rule = null;
            }

            _replacementType = (EShowType)EditorGUILayout.EnumPopup("显示类型", _replacementType, GUILayout.Width(500));
        }
    }

    private void OnGUI()
    {
        // if (Rule != null)
        // {
        //     GUI.enabled = false;
        // }
        //
        // _resShader = (Shader)EditorGUILayout.ObjectField("源shader", _resShader, typeof(Shader), false, GUILayout.Width(500));
        // // if (Rule != null)
        // // {
        // //     GUI.enabled = true;
        // // }
        // //EditorGUI.BeginChangeCheck();
        // _destShader = (Shader)EditorGUILayout.ObjectField("目标shader", _destShader, typeof(Shader), false, GUILayout.Width(500));
        // // if (EditorGUI.EndChangeCheck())
        // // {
        // //     if (Rule)
        // //         Rule.SetShader(_destShader, false);
        // //     ResetCachePropInfo(false);
        // // }
        // EditorGUI.BeginChangeCheck();
        // GUI.enabled = true;
        // Rule = (ShaderReplaceRule)EditorGUILayout.ObjectField("替换规则", Rule, typeof(ShaderReplaceRule), false, GUILayout.Width(500), GUILayout.Height(20));
        // if (EditorGUI.EndChangeCheck())
        // {
        //     if (Rule != null)
        //     {
        //         _resShader = Rule._resShader;
        //         _destShader = Rule._destShader;
        //         _resPorts.Clear();
        //         _destPorts.Clear();
        //     }
        //     else
        //     {
        //         _resShader = null;
        //         _destShader = null;
        //         _resPorts.Clear();
        //         _destPorts.Clear();
        //     }
        // }
        //
        // if (Rule == null)
        // {
        //     if (GUILayout.Button("新建规则", GUILayout.Width(500)))
        //     {
        //         if (_resShader == null)
        //         {
        //             Debug.LogError("请先选择一个源shader");
        //             return;
        //         }
        //
        //         int index = _resShader.name.LastIndexOf("/");
        //         string name = _resShader.name.Substring(index);
        //         string assetName = $"{name}_replacement.asset";
        //         var userSelectPath = EditorUtility.SaveFilePanelInProject("Save shaderReplacementRule:", "Assets", ".asset")
        //         string path = +assetName;
        //         var savedCache = AssetDatabase.LoadAssetAtPath<ShaderReplaceRule>(path);
        //         if (savedCache != null)
        //         {
        //             Debug.LogError($"shader:{name}已经有替换规则了，请勿重复创建");
        //             return;
        //         }
        //         else
        //         {
        //             Rule = ScriptableObject.CreateInstance<ShaderReplaceRule>();
        //             AssetDatabase.CreateAsset(Rule, path);
        //             Rule.SetShader(_resShader, true);
        //             Rule.SetShader(_destShader, false);
        //             ResetCachePropInfo(true);
        //             ResetCachePropInfo(false);
        //         }
        //     }
        // }

        DrawBaseInfo();

        if (Rule != null)
        {
            //检测鼠标输入
            Control();
            //绘制shader属性控制控件
            DrawPropertyInfoControll();
            //绘制shader属性
            Rect mappingRect = DrawPropertyInfo();
            //绘制替换后的material列表
            DrawReplacedMaterials(mappingRect);
        }
    }

    private void Control()
    {
        var e = Event.current;
        var mousePos = GetMousePositionInScrollView();
        switch (e.type)
        {
            case EventType.MouseDown:
                if (_draggingIndex == -1 && e.button == 0)
                {
                    _draggingIndex = GetIndexCotainsMousePosition(mousePos, true);
                }

                break;
            case EventType.MouseUp:
                if (_draggingIndex != -1)
                {
                    if (e.button == 0)
                    {
                        int i = GetIndexCotainsMousePosition(mousePos, false);
                        if (i != -1)
                        {
                            var resPropInfo = Rule.resPropertyInfoList[_draggingIndex];
                            var resType = resPropInfo.type;
                            var destPropInfo = Rule.destPropertyInfoList[i];
                            var destType = destPropInfo.type;
                            if (IsShaderPropertyTypeEqual(resType, destType))
                            {
                                bool destHasBeenUse = IsDestHasBeenConnect(i);
                                if (!destHasBeenUse)
                                    Rule.SetMapping(_draggingIndex, i);
                                else
                                    Debug.LogError(
                                        $"目标属性已经被指定过了！源属性:<color=red>{(_replacementType == EShowType.Name ? resPropInfo.name : resPropInfo.desc)}</color> 目标属性:<color=red>{(_replacementType == EShowType.Name ? destPropInfo.name : destPropInfo.desc)}</color>");
                            }
                            else
                            {
                                Debug.LogError(
                                    $"类型不匹配！源属性:<color=red>{(_replacementType == EShowType.Name ? resPropInfo.name : resPropInfo.desc)}</color> 源类型:<color=red>{resPropInfo.type}</color> " +
                                    $"目标属性:<color=red>{(_replacementType == EShowType.Name ? destPropInfo.name : destPropInfo.desc)}</color> 目标类型<color=red>{destPropInfo.type}</color>");
                            }
                        }

                        _draggingIndex = -1;
                    }
                }
                else
                {
                    if (e.button == 1)
                    {
                        //尝试根据鼠标位置找到源属性的index
                        int resIndex = GetIndexCotainsMousePosition(mousePos, true);
                        if (resIndex == -1)
                        {
                            var destIndex = GetIndexCotainsMousePosition(mousePos, false);
                            if (destIndex != -1)
                            {
                                for (int i = 0; i < Rule.propertyMapping.Count; i++)
                                {
                                    if (Rule.propertyMapping[i] == destIndex)
                                    {
                                        resIndex = i;
                                    }
                                }
                            }
                        }

                        if (resIndex != -1)
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("break"), false, () => { Rule.SetMapping(resIndex, -1); });
                            menu.DropDown(new Rect(mousePos, Vector2.zero));
                        }
                    }
                }

                break;
            default:
                break;
        }
    }

    //目标port是否已经被连接了
    private bool IsDestHasBeenConnect(int i)
    {
        bool destHasBeenUse = false;
        foreach (var destIndex in Rule.propertyMapping)
        {
            if (destIndex == i)
                destHasBeenUse = true;
        }

        return destHasBeenUse;
    }

    private int GetIndexCotainsMousePosition(Vector2 mousePos, bool isRes)
    {
        var ports = isRes ? _resPorts : _destPorts;
        for (int i = 0; i < ports.Count; i++)
        {
            if (GetPortAbsolutelyRect(ports[i]).Contains(mousePos))
            {
                return i;
            }
        }

        return -1;
    }

    private Vector2 GetMousePositionInScrollView()
    {
        var mousePos = Event.current.mousePosition;
        if (_scrollArea.Contains(mousePos))
        {
            mousePos = mousePos + _scrollRoot;
        }

        return mousePos;
    }

    private Rect GetPortAbsolutelyRect(Rect r)
    {
        return new Rect(_scrollArea.position + r.position, r.size);
    }

    private Rect DrawPropertyInfo()
    {
        if (Event.current.type == EventType.Repaint)
        {
            _ruleRect = GUILayoutUtility.GetLastRect();
        }

        var lastRect = _ruleRect;
        lastRect.y += lastRect.height + 10;
        lastRect.x += 20;
        lastRect.width = 700;
        lastRect.height = Screen.height;
        GUI.Box(lastRect, "属性映射");
        Rect mappingRect = lastRect;
        var height = CalculatePropertyHeight() + 20;
        _scrollArea = new Rect(lastRect.x + 20, lastRect.y + 20, lastRect.width - 20, Mathf.Min(height, Screen.height - lastRect.y - 50));
        _scrollRoot = GUI.BeginScrollView(_scrollArea, _scrollRoot, new Rect(0, 0, 400, height));
        lastRect = new Rect(0, 0, 0, 0);
        var searchRect = new Rect(lastRect.x, lastRect.y, 220, 20);
        EditorGUI.BeginChangeCheck();
        _resSearchStr = EditorGUI.TextField(searchRect, "search:", _resSearchStr);
        if (EditorGUI.EndChangeCheck())
        {
            _resPorts.Clear();
        }

        DrawProperties(searchRect, true);
        lastRect.x += 420;
        searchRect = new Rect(lastRect.x, lastRect.y, 220, 20);
        EditorGUI.BeginChangeCheck();
        _destSearchStr = EditorGUI.TextField(searchRect, "search:", _destSearchStr);
        if (EditorGUI.EndChangeCheck())
        {
            _destPorts.Clear();
        }

        DrawProperties(searchRect, false);
        DrawMapping();
        DrawMouseDragingLine();
        GUI.EndScrollView();
        return mappingRect;
    }

    private void DrawProperties(Rect lastRect, bool isRes)
    {
        lastRect = new Rect(lastRect.x, lastRect.y + lastRect.height, 220, 30);
        var shader = isRes ? _resShader : _destShader;
        var propertyInfoList = isRes ? Rule.resPropertyInfoList : Rule.destPropertyInfoList;
        var portXOffset = isRes ? lastRect.width - 6 : -6;
        var ports = isRes ? _resPorts : _destPorts;
        var searchStr = isRes ? _resSearchStr : _destSearchStr;
        bool isRecalculatePort = ports.Count == 0;

        if (shader != null)
        {
            int index = 0;
            foreach (var info in propertyInfoList)
            {
                info.visible = true;
                if (_isOnlyShowConnect)
                {
                    if (isRes)
                    {
                        info.visible = Rule.propertyMapping[index] != -1;
                    }
                    else
                    {
                        info.visible = IsDestHasBeenConnect(index);
                    }
                }

                string showText = null;
                switch (_replacementType)
                {
                    case EShowType.Name:
                        showText = info.name;
                        break;
                    case EShowType.Description:
                        showText = info.desc;
                        break;
                    default:
                        break;
                }

                if (!string.IsNullOrEmpty(searchStr))
                {
                    if (!showText.ToLower().Contains(searchStr.ToLower()))
                    {
                        info.visible = false;
                    }
                }

                if (info.visible)
                {
                    if (_showType)
                        showText += $":<color=red>{info.type.ToString()}</color>";
                    EditorGUI.LabelField(lastRect, showText, new GUIStyle("frameBox"));
                    var c = GUI.color;
                    GUI.color = Color.red;
                    Rect portRect = new Rect(lastRect.x + portXOffset, lastRect.y + lastRect.height / 2 - 6, 12, 12);
                    //GUI.DrawTexture(portRect, dot);
                    Handles.DrawSolidDisc(portRect.position, Vector3.forward, 6);
                    if (isRecalculatePort)
                    {
                        ports.Add(portRect);
                    }

                    GUI.color = c;
                    lastRect.y += lastRect.height + _propInterval;
                }
                else
                {
                    if (isRecalculatePort)
                    {
                        ports.Add(Rect.zero);
                    }
                }

                index++;
            }
        }
    }

    private void DrawMapping()
    {
        for (int i = 0; i < Rule.propertyMapping.Count; i++)
        {
            if (Rule.resPropertyInfoList[i].visible)
            {
                var destIndex = Rule.propertyMapping[i];
                if (destIndex >= 0)
                {
                    if (Rule.destPropertyInfoList.Count > 0 && destIndex < Rule.destPropertyInfoList.Count && Rule.destPropertyInfoList[destIndex].visible)
                    {
                        Rect resPortRect = _resPorts[i];
                        Rect destPortRect = _destPorts[destIndex];
                        Handles.DrawAAPolyLine(resPortRect.position + new Vector2(resPortRect.size.x / 2, resPortRect.size.y / 2),
                            destPortRect.position + new Vector2(resPortRect.size.x / 2, resPortRect.size.y / 2));
                    }
                }
            }
        }
    }

    private void DrawMouseDragingLine()
    {
        if (_draggingIndex != -1)
        {
            Repaint();
            var resPortRect = _resPorts[_draggingIndex];
            Handles.DrawAAPolyLine(resPortRect.position + new Vector2(resPortRect.size.x / 2, resPortRect.size.y / 2), Event.current.mousePosition);
        }
    }

    private void DrawPropertyInfoControll()
    {
        EditorGUILayout.BeginHorizontal();
        _showType = EditorGUILayout.ToggleLeft("展示属性类型", _showType, GUILayout.Width(100));
        EditorGUI.BeginChangeCheck();
        _isOnlyShowConnect = EditorGUILayout.ToggleLeft("仅显示已连接", _isOnlyShowConnect, GUILayout.Width(100));
        if (EditorGUI.EndChangeCheck())
        {
            _resPorts.Clear();
            _destPorts.Clear();
        }

        if (GUILayout.Button("清除属性映射", GUILayout.Width(150)))
        {
            if (EditorUtility.DisplayDialog("警告!", "你确定要清除映射吗?", "确定", "取消"))
            {
                Rule.ResetMapping();
                Repaint();
            }
        }

        if (GUILayout.Button("按照名字映射", GUILayout.Width(150)))
        {
            int i = 0;
            foreach (var resPi in Rule.resPropertyInfoList)
            {
                _replacementType = EShowType.Name;
                int j = 0;
                foreach (var destPi in Rule.destPropertyInfoList)
                {
                    if (destPi.name == resPi.name && IsShaderPropertyTypeEqual(destPi.type, resPi.type) && Rule.propertyMapping[i] == -1 && !IsDestHasBeenConnect(j))
                    {
                        Rule.SetMapping(i, j);
                    }

                    j++;
                }

                i++;
            }
        }

        if (GUILayout.Button("按照描述映射", GUILayout.Width(150)))
        {
            _replacementType = EShowType.Description;
            int i = 0;
            foreach (var resPi in Rule.resPropertyInfoList)
            {
                int j = 0;
                foreach (var destPi in Rule.destPropertyInfoList)
                {
                    if (destPi.desc == resPi.desc && IsShaderPropertyTypeEqual(destPi.type, resPi.type) && Rule.propertyMapping[i] == -1 && !IsDestHasBeenConnect(j))
                    {
                        Rule.SetMapping(i, j);
                    }

                    j++;
                }

                i++;
            }
        }

        if (GUILayout.Button("替换shader", GUILayout.Width(150)))
        {
            ReplaceShader();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ReplaceShader()
    {
        if (Rule.DestShader == null)
        {
            Debug.LogError("目标shader为空，无法替换!");
            return;
        }

        _replacedMat.Clear();
        string folder = EditorUtility.OpenFolderPanel("替换路径", "Assets", "");
        if (string.IsNullOrEmpty(folder))
            return;
        var pathIndex = folder.LastIndexOf("Assets/");
        folder = folder.Substring(pathIndex);
        string[] guids = AssetDatabase.FindAssets("t:material", new[] { folder });
        if (guids == null || guids.Length == 0)
        {
            Debug.LogError("没有找到任何源shader对应的材质");
            return;
        }

        Dictionary<int, object> resMatProp = new Dictionary<int, object>();
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (m != null && m.shader == Rule.ResShader && AssetDatabase.IsMainAsset(m))
            {
                EditorUtility.SetDirty(m);
                resMatProp.Clear();
                for (int index = 0; index < Rule.propertyMapping.Count; index++)
                {
                    if (Rule.propertyMapping[index] != -1)
                    {
                        try
                        {
                            var info = Rule.resPropertyInfoList[index];
                            switch (info.type)
                            {
                                case ShaderPropertyType.Color:
                                    var color = m.GetColor(info.name);
                                    resMatProp.Add(index, color);
                                    break;
                                case ShaderPropertyType.Vector:
                                    var vector = m.GetVector(info.name);
                                    resMatProp.Add(index, vector);
                                    break;
                                case ShaderPropertyType.Float:
                                    var f = m.GetFloat(info.name);
                                    resMatProp.Add(index, f);
                                    break;
                                case ShaderPropertyType.Range:
                                    var i = m.GetFloat(info.name);
                                    resMatProp.Add(index, i);
                                    break;
                                case ShaderPropertyType.Texture:
                                    object[] textureInfo = new object[3];
                                    var texture = m.GetTexture(info.name);
                                    var tiling = m.GetTextureScale(info.name);
                                    var offset = m.GetTextureOffset(info.name);
                                    textureInfo[0] = texture;
                                    textureInfo[1] = tiling;
                                    textureInfo[2] = offset;
                                    resMatProp.Add(index, textureInfo);
                                    break;
                                case ShaderPropertyType.Int:
                                    var j = m.GetInteger(info.name);
                                    resMatProp.Add(index, j);
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"读取材质时出现异常！res shader prop index:{index} {e.Message} \r\n {e.StackTrace}");
                        }
                    }
                }

                m.shader = Rule.DestShader;
                foreach (var kv in resMatProp)
                {
                    try
                    {
                        var index = kv.Key;
                        var destIndex = Rule.propertyMapping[index];
                        if (destIndex != -1)
                        {
                            var info = Rule.destPropertyInfoList[destIndex];
                            switch (info.type)
                            {
                                case ShaderPropertyType.Color:
                                    Color color = (Color)kv.Value;
                                    m.SetColor(info.name, color);
                                    break;
                                case ShaderPropertyType.Vector:
                                    Vector4 vector = (Vector4)kv.Value;
                                    m.SetVector(info.name, vector);
                                    break;
                                case ShaderPropertyType.Float:
                                    float f = (float)kv.Value;
                                    m.SetFloat(info.name, f);
                                    break;
                                case ShaderPropertyType.Range:
                                    float i = (float)kv.Value;
                                    m.SetFloat(info.name, i);
                                    break;
                                case ShaderPropertyType.Texture:
                                    object[] textureInfo = (object[])kv.Value;
                                    m.SetTexture(info.name, (Texture)textureInfo[0]);
                                    m.SetTextureScale(info.name, (Vector2)textureInfo[1]);
                                    m.SetTextureOffset(info.name, (Vector2)textureInfo[2]);
                                    break;
                                case ShaderPropertyType.Int:
                                    int j = (int)kv.Value;
                                    m.SetInteger(info.name, j);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"写入材质时出现异常！res shader prop index:{kv.Key} {e.Message} \r\n {e.StackTrace}");
                    }
                }

                _replacedMat.Add(m);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("替换成功！");
            Repaint();
        }
    }

    private void DrawReplacedMaterials(Rect lastRect)
    {
        if (_replacedMat.Count > 0)
        {
            lastRect.x += lastRect.width + 20;
            lastRect.width = 220;
            GUI.Box(lastRect, "替换材质列表");
            var height = _replacedMat.Count * 20;
            var scrollArea = new Rect(lastRect.x + 20, lastRect.y + 20, lastRect.width - 20, Screen.height - lastRect.y - 50);
            _matScrollRoot = GUI.BeginScrollView(scrollArea, _matScrollRoot, new Rect(0, 0, 150, height));
            lastRect = new Rect(0, 0, 200, 20);
            GUI.enabled = false;
            foreach (var mat in _replacedMat)
            {
                EditorGUI.ObjectField(lastRect, mat, typeof(Material), false);
                lastRect.y += lastRect.height;
            }

            GUI.enabled = true;
            GUI.EndScrollView();
        }
    }

    // private void ResetCachePropInfo(bool isRes)
    // {
    //     if (Rule != null)
    //     {
    //         List<ShaderPropInfo> shaderInfoList = null;
    //         Shader shader = null;
    //         List<Rect> ports = null;
    //         if (isRes)
    //         {
    //             shaderInfoList = Rule.resPropertyInfoList;
    //             shader = _resShader;
    //             ports = _resPorts;
    //         }
    //         else
    //         {
    //             shaderInfoList = Rule.destPropertyInfoList;
    //             shader = _destShader;
    //             ports = _destPorts;
    //         }
    //
    //         shaderInfoList.Clear();
    //         ports.Clear();
    //         if (shader != null)
    //         {
    //             int count = shader.GetPropertyCount();
    //             for (int i = 0; i < count; i++)
    //             {
    //                 ShaderPropInfo pi = new ShaderPropInfo();
    //                 pi.name = shader.GetPropertyName(i);
    //                 pi.desc = shader.GetPropertyDescription(i);
    //                 pi.type = shader.GetPropertyType(i);
    //                 shaderInfoList.Add(pi);
    //             }
    //         }
    //
    //         Rule.ResetMapping();
    //     }
    // }

    private float CalculatePropertyHeight()
    {
        float h1 = 0;
        float h2 = 0;
        if (_resShader != null)
        {
            var count = _resShader.GetPropertyCount();
            h1 = count * _propHeight + (count - 1) * _propInterval;
        }

        if (_destShader != null)
        {
            var count = _destShader.GetPropertyCount();
            h2 = count * _propHeight + (count - 1) * _propInterval;
        }

        return Mathf.Max(h1, h2);
    }

    private bool IsShaderPropertyTypeEqual(ShaderPropertyType t1, ShaderPropertyType t2)
    {
        if ((t1 == ShaderPropertyType.Range || t1 == ShaderPropertyType.Float) &&
            (t2 == ShaderPropertyType.Range || t2 == ShaderPropertyType.Float))
        {
            return true;
        }

        return t1 == t2;
    }
}
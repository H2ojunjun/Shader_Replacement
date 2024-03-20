using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEngine.Rendering;

namespace ShaderReplacement
{
    public class ShaderReplacementWindow : EditorWindow
    {
        #region InnerClass

        private enum EMappingType
        {
            Name,
            Description
        }

        private class MaterialInfo
        {
            public Material material;
            public bool isSelected = true;

            public MaterialInfo(Material material)
            {
                this.material = material;
            }
        }

        #endregion

        #region FieldsAndProperties

        private Shader _resShader;
        private Shader _destShader;
        private ShaderReplaceRule _rule;
        private EMappingType _replacementType = EMappingType.Description;
        private Vector2 _scrollRoot = Vector2.zero;
        private Vector2 _matScrollRoot = Vector2.zero;
        private float _propHeight = 30;
        private float _propInterval = 3;
        private List<Rect> _resPorts = new List<Rect>();
        private List<Rect> _destPorts = new List<Rect>();
        private Rect _ruleRect;
        private Rect _scrollArea;
        private int _draggingIndex = -1;
        private bool _showType = true;
        private bool _isOnlyShowConnect = false;
        private List<MaterialInfo> _materialsWithResShader = new();
        private bool _selectedAllMat = true;
        private string _resSearchStr;
        private string _destSearchStr;

        private ShaderReplaceRule Rule
        {
            get => _rule;
            set
            {
                if (_rule != value)
                {
                    _rule = value;
                    _materialsWithResShader.Clear();
                    if (_rule != null)
                    {
                        _resShader = _rule.ResShader;
                        _destShader = _rule.DestShader;
                        CollectAllMaterialWithResShader();
                        Mapping(EMappingType.Name);
                        SelectedAllMat = true;
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

        private bool SelectedAllMat
        {
            get => _selectedAllMat;
            set
            {
                if (_selectedAllMat != value)
                {
                    _selectedAllMat = value;
                    foreach (var matInfo in _materialsWithResShader)
                    {
                        matInfo.isSelected = value;
                    }
                }
            }
        }

        #endregion

        #region ShowWindow

        [MenuItem("Tools/ShaderReplacement")]
        private static void ShowWindow()
        {
            var window = (ShaderReplacementWindow)GetWindow<ShaderReplacementWindow>("Shader Replacement", true);
            window.Show();
        }

        #endregion
        
        #region Draw

        private void OnGUI()
        {
            DrawBaseInfo();
            if (Rule != null)
            {
                Control();
                //绘制shader属性控制控件
                DrawPropertyInfoControl();
                //绘制shader属性
                Rect mappingRect = DrawPropertyInfo();
                //绘制替换后的material列表
                DrawResShaderMaterials(mappingRect);
            }
        }

        /// <summary>
        /// 绘制基础信息
        /// </summary>
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
            if (Rule == null)
            {
                if (_resShader != null && _destShader != null && _resShader != _destShader && GUILayout.Button("开始替换", GUILayout.Width(500), GUILayout.Height(20)))
                {
                    var rule = new ShaderReplaceRule(_resShader, _destShader);
                    Rule = rule;
                }
            }
            else
            {
                if (GUILayout.Button("重置", GUILayout.Width(500), GUILayout.Height(20)))
                {
                    Rule = null;
                }

                _replacementType = (EMappingType)EditorGUILayout.EnumPopup("显示类型", _replacementType, GUILayout.Width(500));
            }
        }

        /// <summary>
        /// 绘制属性映射的控制按钮
        /// </summary>
        private void DrawPropertyInfoControl()
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
                    ClearMapping();
                }
            }

            if (GUILayout.Button("按照名字映射", GUILayout.Width(150)))
            {
                Mapping(EMappingType.Name);
            }

            if (GUILayout.Button("按照描述映射", GUILayout.Width(150)))
            {
                Mapping(EMappingType.Description);
            }

            if (GUILayout.Button("替换shader", GUILayout.Width(150)))
            {
                ReplaceShader();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制源Shader和目标Shader的属性映射整体布局
        /// </summary>
        /// <returns></returns>
        private Rect DrawPropertyInfo()
        {
            //只有当前Event为Repaint的时候才能调用GUILayoutUtility.GetLastRect()
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
            DrawMouseDraggingLine();
            GUI.EndScrollView();
            return mappingRect;
        }

        /// <summary>
        /// 绘制源Shader或者目标Shader的属性列表
        /// </summary>
        /// <param name="lastRect"></param>
        /// <param name="isRes"></param>
        private void DrawProperties(Rect lastRect, bool isRes)
        {
            lastRect = new Rect(lastRect.x, lastRect.y + lastRect.height, 220, 30);
            var shader = isRes ? _resShader : _destShader;
            var propertyInfoList = isRes ? Rule.resPropertyInfoList : Rule.destPropertyInfoList;
            var portXOffset = isRes ? lastRect.width - 6 : -6;
            var portYOffset = lastRect.height / 2 - 6;
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

                    string showText = "";
                    switch (_replacementType)
                    {
                        case EMappingType.Name:
                            showText = info.name;
                            break;
                        case EMappingType.Description:
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
                        var c = Handles.color;
                        Handles.color = Color.red;
                        Rect portRect = new Rect(lastRect.x + portXOffset, lastRect.y + portYOffset, 12, 12);
                        Handles.DrawSolidDisc(portRect.position + new Vector2(portRect.width / 2, portRect.height / 2), Vector3.forward, 6);
                        if (isRecalculatePort)
                        {
                            ports.Add(portRect);
                        }

                        Handles.color = c;
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

        /// <summary>
        /// 绘制源Shader和目标Shader的映射关系
        /// </summary>
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
                            Handles.DrawAAPolyLine(resPortRect.position + new Vector2(resPortRect.width / 2, resPortRect.height / 2),
                                destPortRect.position + new Vector2(destPortRect.width / 2, destPortRect.height / 2));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制鼠标正拖拽的port的连线
        /// </summary>
        private void DrawMouseDraggingLine()
        {
            if (_draggingIndex != -1)
            {
                Repaint();
                var resPortRect = _resPorts[_draggingIndex];
                Handles.DrawAAPolyLine(resPortRect.position + new Vector2(resPortRect.width / 2, resPortRect.height / 2), Event.current.mousePosition);
            }
        }

        /// <summary>
        /// 绘制使用源Shader的材质
        /// </summary>
        /// <param name="lastRect"></param>
        private void DrawResShaderMaterials(Rect lastRect)
        {
            if (_materialsWithResShader.Count > 0)
            {
                lastRect.x += lastRect.width + 20;
                lastRect.width = 220;
                GUI.Box(lastRect, "替换材质列表");
                lastRect.y += 20;
                lastRect.x += 20;
                SelectedAllMat = GUI.Toggle(new Rect(lastRect.position, new Vector2(80, 20)), SelectedAllMat, "全选/反选");
                lastRect.y += 20;
                var height = _materialsWithResShader.Count * 20;
                var scrollArea = new Rect(lastRect.x, lastRect.y, lastRect.width - 20, Screen.height - lastRect.y - 50);
                _matScrollRoot = GUI.BeginScrollView(scrollArea, _matScrollRoot, new Rect(0, 0, 150, height));
                lastRect = new Rect(0, 0, 200, 20);
                foreach (var matInfo in _materialsWithResShader)
                {
                    var toggleRect = new Rect(lastRect.position, new Vector2(20, 20));
                    matInfo.isSelected = EditorGUI.Toggle(toggleRect, matInfo.isSelected);
                    var labelRect = new Rect(lastRect.x + 20, lastRect.y, 180, 20);
                    EditorGUI.ObjectField(labelRect, matInfo.material, typeof(Material), false);

                    lastRect.y += lastRect.height;
                }

                GUI.EndScrollView();
            }
        }

        /// <summary>
        /// 计算装满所有属性所需要的高度
        /// </summary>
        /// <returns></returns>
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

        #endregion

        #region Logic

        /// <summary>
        /// 输入控制
        /// </summary>
        private void Control()
        {
            var e = Event.current;
            var mousePos = GetMousePositionInScrollView();
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (_draggingIndex == -1 && e.button == 0)
                    {
                        _draggingIndex = GetPortContainsMousePosition(mousePos, true);
                    }

                    break;
                case EventType.MouseUp:
                    if (_draggingIndex != -1)
                    {
                        if (e.button == 0)
                        {
                            int i = GetPortContainsMousePosition(mousePos, false);
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
                                            $"目标属性已经被指定过了！源属性:<color=red>{(_replacementType == EMappingType.Name ? resPropInfo.name : resPropInfo.desc)}</color> 目标属性:<color=red>{(_replacementType == EMappingType.Name ? destPropInfo.name : destPropInfo.desc)}</color>");
                                }
                                else
                                {
                                    Debug.LogError(
                                        $"类型不匹配！源属性:<color=red>{(_replacementType == EMappingType.Name ? resPropInfo.name : resPropInfo.desc)}</color> 源类型:<color=red>{resPropInfo.type}</color> " +
                                        $"目标属性:<color=red>{(_replacementType == EMappingType.Name ? destPropInfo.name : destPropInfo.desc)}</color> 目标类型<color=red>{destPropInfo.type}</color>");
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
                            int resIndex = GetPortContainsMousePosition(mousePos, true);
                            if (resIndex == -1)
                            {
                                var destIndex = GetPortContainsMousePosition(mousePos, false);
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
                                menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                            }
                        }
                    }

                    break;
                default:
                    break;
            }
        }

        //收集所有使用了源Shader的材质
        private void CollectAllMaterialWithResShader()
        {
            _materialsWithResShader.Clear();
            var paths = AssetDatabase.FindAssets("t:material", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath);
            foreach (var path in paths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat.shader == _resShader && AssetDatabase.IsMainAsset(mat))
                {
                    _materialsWithResShader.Add(new(mat));
                }
            }
        }
        
        /// <summary>
        /// 清除所有映射关系
        /// </summary>
        private void ClearMapping()
        {
            Rule.ResetMapping();
        }
        
        /// <summary>
        /// 根据某种方式映射
        /// </summary>
        /// <param name="mappingType"></param>
        private void Mapping(EMappingType mappingType)
        {
            int i = 0;
            _replacementType = mappingType;
            foreach (var resPi in Rule.resPropertyInfoList)
            {
                int j = 0;
                foreach (var destPi in Rule.destPropertyInfoList)
                {
                    var isMappingTypeMatch = false;
                    switch (mappingType)
                    {
                        case EMappingType.Name:
                            isMappingTypeMatch = destPi.name == resPi.name;
                            break;
                        case EMappingType.Description:
                            isMappingTypeMatch = resPi.desc == destPi.desc;
                            break;
                    }

                    if (isMappingTypeMatch && IsShaderPropertyTypeEqual(destPi.type, resPi.type) && Rule.propertyMapping[i] == -1 && !IsDestHasBeenConnect(j))
                    {
                        Rule.SetMapping(i, j);
                    }

                    j++;
                }

                i++;
            }
        }

        /// <summary>
        /// 替换选中材质的Shader
        /// </summary>
        private void ReplaceShader()
        {
            Dictionary<int, object> resMatProp = new Dictionary<int, object>();
            foreach (var matInfo in _materialsWithResShader)
            {
                if (!matInfo.isSelected)
                    continue;
                Material m = matInfo.material;
                var enableGPUInstancing = m.enableInstancing;
                var renderQueue = m.renderQueue;
                var doubleSidedGI = m.doubleSidedGI;
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
                ClearMaterialProperties(m, Rule.destPropertyInfoList);
                m.enableInstancing = enableGPUInstancing;
                m.renderQueue = renderQueue;
                m.doubleSidedGI = doubleSidedGI;

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
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("替换成功！");
            CollectAllMaterialWithResShader();
            Repaint();
        }

        #endregion

        #region Utilities
        
        /// <summary>
        /// 目标port是否已经被连接了
        /// </summary>
        /// <param name="i">port id</param>
        /// <returns></returns>
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

        /// <summary>
        /// 获得当前鼠标位置上的port,没有port则返回-1
        /// </summary>
        /// <param name="mousePos">鼠标位置</param>
        /// <param name="isRes">是否源Shader</param>
        /// <returns></returns>
        private int GetPortContainsMousePosition(Vector2 mousePos, bool isRes)
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

        /// <summary>
        /// mousePos + ScrollRect滑动条的值，等于绝对坐标
        /// </summary>
        /// <returns></returns>
        private Vector2 GetMousePositionInScrollView()
        {
            var mousePos = Event.current.mousePosition;
            if (_scrollArea.Contains(mousePos))
            {
                mousePos += _scrollRoot;
            }

            return mousePos;
        }

        /// <summary>
        /// 获取port的绝对坐标，portRect中保存的值是ScrollRect中的局部坐标
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private Rect GetPortAbsolutelyRect(Rect r)
        {
            return new Rect(_scrollArea.position + r.position, r.size);
        }

        /// <summary>
        /// Shader替换时的类型检查
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        private bool IsShaderPropertyTypeEqual(ShaderPropertyType t1, ShaderPropertyType t2)
        {
            if ((t1 == ShaderPropertyType.Range || t1 == ShaderPropertyType.Float) &&
                (t2 == ShaderPropertyType.Range || t2 == ShaderPropertyType.Float))
            {
                return true;
            }

            return t1 == t2;
        }

        /// <summary>
        /// 将材质上所有的属性值都设置为默认值，防止替换shader后同名属性自动映射
        /// </summary>
        private void ClearMaterialProperties(Material mat,List<ShaderPropInfo> shaderPropInfos)
        {
            foreach (var shaderPropInfo in shaderPropInfos)
            {
                switch (shaderPropInfo.type)
                {
                    case ShaderPropertyType.Color:
                        var vec = (Vector4)shaderPropInfo.defaultValue;
                        mat.SetColor(shaderPropInfo.name, new Color(vec.x, vec.y, vec.z, vec.w));
                        break;
                    case ShaderPropertyType.Vector:
                        mat.SetVector(shaderPropInfo.name, (Vector4)shaderPropInfo.defaultValue);
                        break;
                    case ShaderPropertyType.Float:
                        mat.SetFloat(shaderPropInfo.name, (float)shaderPropInfo.defaultValue);
                        break;
                    case ShaderPropertyType.Range:
                        mat.SetFloat(shaderPropInfo.name, (float)shaderPropInfo.defaultValue);
                        break;
                    case ShaderPropertyType.Texture:
                        mat.SetTexture(shaderPropInfo.name, null);
                        break;
                    case ShaderPropertyType.Int:
                        mat.SetInt(shaderPropInfo.name, (int)shaderPropInfo.defaultValue);
                        break;
                }
            }
        }
        
        #endregion
    }
}
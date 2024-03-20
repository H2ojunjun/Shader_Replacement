using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderReplacement
{
    public class ShaderPropInfo
    {
        public string name;
        public ShaderPropertyType type;
        public string desc;
        public bool visible = true;
        public object defaultValue;
    }

    /// <summary>
    /// 记录源Shader到目标Shader的属性映射
    /// </summary>
    public class ShaderReplaceRule
    {
        private Shader _resShader;
        private Shader _destShader;

        public List<ShaderPropInfo> resPropertyInfoList = new();
        public List<ShaderPropInfo> destPropertyInfoList = new();
        public List<int> propertyMapping = new();

        public Shader ResShader => _resShader;

        public Shader DestShader => _destShader;

        public ShaderReplaceRule(Shader resShader, Shader destShader)
        {
            if (resShader == null || destShader == null)
            {
                Debug.LogError("at least one shader is null");
                return;
            }

            _resShader = resShader;
            _destShader = destShader;
            ResetCachePropInfo();
        }

        public void SetMapping(int resIndex, int destIndex)
        {
            propertyMapping[resIndex] = destIndex;
        }

        public void ResetMapping()
        {
            propertyMapping.Clear();
            if (resPropertyInfoList != null && resPropertyInfoList.Count > 0)
            {
                foreach (var pi in resPropertyInfoList)
                {
                    propertyMapping.Add(-1);
                }
            }
        }

        private void ResetCachePropInfo()
        {
            resPropertyInfoList.Clear();
            destPropertyInfoList.Clear();

            GetShaderPropertyInfo(_resShader, resPropertyInfoList);

            GetShaderPropertyInfo(_destShader, destPropertyInfoList);
            
            ResetMapping();
        }

        private void GetShaderPropertyInfo(Shader shader,List<ShaderPropInfo> shaderPropInfos)
        {
            shaderPropInfos.Clear();
            int count = shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                var type = shader.GetPropertyType(i);
                object defaultValue = null;
                switch (type)
                {
                    case ShaderPropertyType.Color:
                        defaultValue = (object)shader.GetPropertyDefaultVectorValue(i);
                        break;
                    case ShaderPropertyType.Vector:
                        defaultValue = (object)shader.GetPropertyDefaultVectorValue(i);
                        break;
                    case ShaderPropertyType.Float:
                        defaultValue = (object)shader.GetPropertyDefaultFloatValue(i);
                        break;
                    case ShaderPropertyType.Range:
                        defaultValue = (object)shader.GetPropertyDefaultFloatValue(i);
                        break;
                    case ShaderPropertyType.Texture:
                        defaultValue = (object)shader.GetPropertyTextureDefaultName(i);
                        break;
                    case ShaderPropertyType.Int:
                        defaultValue = (object)shader.GetPropertyDefaultIntValue(i);
                        break;
                }
                ShaderPropInfo pi = new ShaderPropInfo
                {
                    name = shader.GetPropertyName(i),
                    desc = shader.GetPropertyDescription(i),
                    type = type,
                    defaultValue = defaultValue,
                };
                shaderPropInfos.Add(pi);
            }
        }
    }
}
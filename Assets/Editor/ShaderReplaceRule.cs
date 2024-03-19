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
    }

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
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LLMUnityFuncCall
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SchemaArgTypeAttribute : Attribute
    {
        public Type ArgType;
        public SchemaArgTypeAttribute(Type argType)
        {
            ArgType = argType;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    public class SchemaDescriptionAttribute : Attribute
    {
        public string Content;
        public SchemaDescriptionAttribute(string content) { Content = content; }
        public string GetEscapedContent()
        {
            return Content.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("'", "\\'").Replace("\"", "\\\"")
                .Replace("\b", "\\b").Replace("\f", "\\f").Replace("\t", "\\t").Replace("/", "\\/");
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SchemaRequiredAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SchemaNameAttribute : Attribute
    {
        public string Name;
        public SchemaNameAttribute(string name)
        {
            Name = name;
        }
    }
}

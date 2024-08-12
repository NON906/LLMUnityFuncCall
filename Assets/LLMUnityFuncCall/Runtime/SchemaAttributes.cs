using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LLMUnityFuncCall
{
    [AttributeUsage(AttributeTargets.Method)]
    class SchemaArgTypeAttribute : Attribute
    {
        public Type ArgType;
        public SchemaArgTypeAttribute(Type argType)
        {
            ArgType = argType;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    class SchemaDescriptionAttribute : Attribute
    {
        public string Content;
        public SchemaDescriptionAttribute(string content) { Content = content; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    class SchemaRequiredAttribute : Attribute
    {
    }
}

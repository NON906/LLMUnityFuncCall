using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LLMUnityFuncCall
{
    public static class SchemaBuilder
    {
        static string generateObjectSchemaData(Type membersType)
        {
            var members = membersType.GetFields();

            string schemaData = "";
            foreach (var member in members)
            {
                schemaData += "'" + member.Name + "': ";

                schemaData += generateTypeSchemaData(member.FieldType);
                var attrs = (SchemaDescriptionAttribute[])member.GetCustomAttributes(typeof(SchemaDescriptionAttribute), true);
                if (attrs.Length > 0)
                {
                    schemaData = schemaData.Remove(schemaData.Length - 1);
                    schemaData += ", 'description': '" + attrs[0].GetEscapedContent() + "'}";
                }
                schemaData += "}";

                if (member != members[members.Length - 1])
                {
                    schemaData += ", ";
                }
            }

            return schemaData;
        }

        static string generateTypeSchemaData(Type memberType)
        {
            string schemaData = "";

            if (memberType == typeof(bool))
            {
                schemaData += "{'type': 'boolean'}";
            }
            else if (memberType == typeof(string))
            {
                schemaData += "{'type': 'string'}";
            }
            else if (memberType.IsArray || memberType.GetInterfaces().Any(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                schemaData += "{'type': 'array', 'items': ";
                schemaData += generateTypeSchemaData(memberType.GetElementType());
                schemaData += "}";
            }
            else if (memberType.IsPrimitive)
            {
                schemaData += "{'type': 'number'}";
            }
            else if (memberType.IsEnum)
            {
                schemaData += "{'type': 'string'}"; // preliminary
            }
            else
            {
                schemaData += "{'type': 'object', 'properties': ";
                schemaData += generateObjectSchemaData(memberType);

                string required = generateRequired(memberType);
                if (required == "[]")
                {
                    schemaData += "}";
                }
                else
                {
                    schemaData += ", 'required': " + required + "}";
                }
            }

            return schemaData;
        }

        static string generateRequired(Type type)
        {
            var members = type.GetFields();
            string ret = "[";
            foreach (var member in members)
            {
                var reqAttrs = (SchemaRequiredAttribute[])member.GetCustomAttributes(typeof(SchemaRequiredAttribute), true);
                if (reqAttrs.Length > 0)
                {
                    if (ret != "[")
                    {
                        ret += ", ";
                    }
                    ret += "'" + member.Name + "'";
                }
            }
            ret += "]";
            return ret;
        }

        public static string Build(Type type)
        {
            string schemaData = "{";

            string required = "[]";

            schemaData += generateObjectSchemaData(type);

            required = generateRequired(type);

            schemaData += "}";

            return "{'type': 'object', 'properties': " + schemaData + ", 'required': " + required + "}";
        }

        public static string Build<T>()
        {
            return Build(typeof(T));
        }
    }
}

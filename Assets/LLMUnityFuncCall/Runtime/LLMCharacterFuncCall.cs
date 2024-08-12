using LLMUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace LLMUnityFuncCall
{
    public class FuncCallData
    {
        public string output;
        public string argumentsJson;
        public object arguments;
        public Type argumentsType;
    }

    public class LLMCharacterFuncCall : LLMCharacter
    {
        [Serializable]
        class FuncCallBase
        {
            public string name;
        }

        const string SYSTEM_TEMPLATE_BEFORE = "You are a function calling AI model. You are provided with function signatures within <tools></tools> XML tags. You may call one or more functions to assist with the user query. Don't make assumptions about what values to plug into functions. Here are the available tools: <tools> ";
        const string SYSTEM_TEMPLATE_AFTER = " </tools> Use the following pydantic model json schema for each tool call you will make: {\"properties\": {\"arguments\": {\"title\": \"Arguments\", \"type\": \"object\"}, \"name\": {\"title\": \"Name\", \"type\": \"string\"}}, \"required\": [\"arguments\", \"name\"], \"title\": \"FunctionCall\", \"type\": \"object\"} For each function call return a json object with function name and arguments within <tool_call></tool_call> XML tags as follows:\n<tool_call>\n{\"arguments\": <args-dict>, \"name\": <function-name>}\n</tool_call>";

        [SerializeField] UnityEvent<FuncCallData> Tools;

        bool isInsertedSystemMessage_ = false;

        string generateRequired(Type type)
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

        string generateTypeSchemaData(Type memberType)
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
            else if (memberType.IsStruct() || memberType.IsClass)
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
            else if (memberType.IsValueType)
            {
                schemaData += "{'type': 'number'}";
            }
            else
            {
                Debug.LogError("Unknown Type.");
            }

            return schemaData;
        }

        string generateObjectSchemaData(Type membersType)
        {
            var members = membersType.GetFields();

            string schemaData = "";
            foreach (var member in members)
            {
                schemaData += "'" + member.Name + "': ";

                schemaData += generateTypeSchemaData(member.FieldType) + "}";
                if (member != members[members.Length - 1])
                {
                    schemaData += ", ";
                }
            }

            return schemaData;
        }

        string buildToolsSchema()
        {
            string allTools = "[";

            int toolsCount = Tools.GetPersistentEventCount();
            for (int toolsIndex = 0; toolsIndex < toolsCount; toolsIndex++)
            {
                var targetObj = Tools.GetPersistentTarget(toolsIndex);
                string name = Tools.GetPersistentMethodName(toolsIndex);

                var method = targetObj.GetType().GetMethod(name);
                var attrs = (SchemaDescriptionAttribute[])method.GetCustomAttributes(typeof(SchemaDescriptionAttribute), true);
                string description = "";
                if (attrs.Length > 0)
                {
                    description = attrs[0].Content;
                }

                string schemaData = "{";
                var argAttrs = (SchemaArgTypeAttribute[])method.GetCustomAttributes(typeof(SchemaArgTypeAttribute), true);

                string required = "[]";
                if (argAttrs.Length > 0)
                {
                    schemaData += generateObjectSchemaData(argAttrs[0].ArgType);

                    required = generateRequired(argAttrs[0].ArgType);
                }

                schemaData += "}";

                allTools += "{'type': 'function', 'function': {'name': '" + name + "', 'description': '" + description + "', 'parameters': {'type': 'object', 'properties': " + schemaData + ", 'required': " + required + "}}";

                if (toolsIndex < toolsCount - 1)
                {
                    allTools += ", ";
                }
            }

            allTools += "]";

            return allTools;
        }

        string toolCall(string funcCall)
        {
            string funcName = JsonUtility.FromJson<FuncCallBase>(funcCall)?.name;

            FuncCallData data = new FuncCallData();

            int toolsCount = Tools.GetPersistentEventCount();
            for (int toolsIndex = 0; toolsIndex < toolsCount; toolsIndex++)
            {
                string name = Tools.GetPersistentMethodName(toolsIndex);
                if (name != funcName)
                {
                    continue;
                }

                var match = Regex.Match(funcCall, "[\"']arguments[\"']\\s*:\\s*(\\{.*\\})");
                if (!match.Success)
                {
                    continue;
                }
                string argumentsStr = match.Groups[1].Value;
                argumentsStr = argumentsStr.Split('}')[0] + "}";

                data.argumentsJson = argumentsStr;

                var targetObj = Tools.GetPersistentTarget(toolsIndex);
                var method = targetObj.GetType().GetMethod(name);

                var argAttrs = (SchemaArgTypeAttribute[])method.GetCustomAttributes(typeof(SchemaArgTypeAttribute), true);
                if (argAttrs.Length > 0 && argAttrs[0].ArgType != typeof(string))
                {
                    data.arguments = JsonUtility.FromJson(argumentsStr, argAttrs[0].ArgType);
                    if (data.arguments == null)
                    {
                        data.argumentsType = null;
                    }
                    else
                    {
                        data.argumentsType = argAttrs[0].ArgType;
                    }
                }
                else
                {
                    data.argumentsType = null;
                    data.arguments = null;
                }

                method.Invoke(targetObj, new[] { data });
            }

            if (data.output.TrimStart()[0] != '{')
            {
                data.output = "\"" + data.output + "\"";
            }

            return "<tool_response>\n{\"name\": \"" + funcName + "\", \"content\": " + data.output + "}\n</tool_response>\n";
        }

        public async Task<string> ChatWithFuncCall(string query, Callback<string> callback = null, EmptyCallback completionCallback = null, bool addToHistory = true)
        {
            void ignoreFuncCallCallback(string input)
            {
                string trimedInput = input.TrimStart();

                bool notToolCall = true;
                bool checkToolCall = false;
                if (trimedInput.Contains("<tool_call>"))
                {
                    notToolCall = false;
                    checkToolCall = true;
                }
                if ("<tool_call>".Contains(trimedInput))
                {
                    notToolCall = false;
                }

                if (notToolCall)
                {
                    callback(input);
                }
                else if (checkToolCall)
                {
                    if (input.Contains("</tool_call>"))
                    {
                        CancelRequests();
                    }
                }
            }

            if (!isInsertedSystemMessage_ && chat[0].role == "system")
            {
                chat[0] = new ChatMessage { role = "system", content = SYSTEM_TEMPLATE_BEFORE + buildToolsSchema() + SYSTEM_TEMPLATE_AFTER + "\n\n" + chat[0].content };
                isInsertedSystemMessage_ = true;
            }

            int startChatCount = chat.Count;

            bool isFinish = false;
            string chatResult = await Chat(query, ignoreFuncCallCallback, null, true);
            do
            {
                if (chatResult.Contains("<tool_call>") && chatResult.Contains("</tool_call>"))
                {
                    string toolCallJson = chatResult.Split("<tool_call>")[1].Split("</tool_call>")[0];
                    string toolCallResult = toolCall(toolCallJson);
                    AddMessage("tool", toolCallResult);

                    string prompt = new ChatMLTemplate().ComputePrompt(chat, AIName);
                    chatResult = await Complete(prompt, ignoreFuncCallCallback);
                    if (chatResult != null)
                    {
                        AddAIMessage(chatResult);
                    }
                }
                else
                {
                    isFinish = true;
                }
            } while (!isFinish);

            if (!addToHistory)
            {
                chat.RemoveRange(startChatCount, chat.Count - startChatCount);
            }

            completionCallback?.Invoke();
            return chatResult;
        }
    }
}

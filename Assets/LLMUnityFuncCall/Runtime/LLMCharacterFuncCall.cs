using LLMUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public delegate void ToolFunc(FuncCallData data);

        class Tool
        {
            public string name;
            public string originalName;
            public MethodInfo method;
            public UnityEngine.Object target = null;
            public ToolFunc delegateFunc = null;

            public void Call(FuncCallData data)
            {
                if (delegateFunc != null)
                {
                    delegateFunc.Invoke(data);
                }
                else if (target != null)
                {
                    method.Invoke(target, new[] { data });
                }
            }
        }

        [Serializable]
        class FuncCallBase
        {
            public string name;
        }

        const string SYSTEM_TEMPLATE_BEFORE = "You are a function calling AI model. You are provided with function signatures within <tools></tools> XML tags. You may call one or more functions to assist with the user query. Don't make assumptions about what values to plug into functions. Here are the available tools: <tools> ";
        const string SYSTEM_TEMPLATE_AFTER = " </tools> Use the following pydantic model json schema for each tool call you will make: {\"properties\": {\"arguments\": {\"title\": \"Arguments\", \"type\": \"object\"}, \"name\": {\"title\": \"Name\", \"type\": \"string\"}}, \"required\": [\"arguments\", \"name\"], \"title\": \"FunctionCall\", \"type\": \"object\"} For each function call return a json object with function name and arguments within <tool_call></tool_call> XML tags as follows:\n<tool_call>\n{\"arguments\": <args-dict>, \"name\": <function-name>}\n</tool_call>";

        [SerializeField]
        UnityEvent<FuncCallData> Tools;

        List<Tool> tools_ = null;
        bool isInsertedSystemMessage_ = false;

        void initTools()
        {
            if (tools_ != null)
            {
                return;
            }

            tools_ = new List<Tool>();

            int toolsCount = Tools.GetPersistentEventCount();
            for (int toolsIndex = 0; toolsIndex < toolsCount; toolsIndex++)
            {
                Tool newTool = new Tool();
                newTool.target = Tools.GetPersistentTarget(toolsIndex);
                newTool.originalName = Tools.GetPersistentMethodName(toolsIndex);
                newTool.method = newTool.target.GetType().GetMethod(newTool.originalName);
                var attrs = (SchemaNameAttribute[])newTool.method.GetCustomAttributes(typeof(SchemaNameAttribute), true);
                if (attrs.Length > 0)
                {
                    newTool.originalName = attrs[0].Name;
                }
                newTool.name = newTool.originalName;

                tools_.Add(newTool);
            }

            refreshToolName();
        }

        string getFullName(Transform objTrans)
        {
            if (objTrans.parent == null)
            {
                return objTrans.gameObject.name;
            }
            return getFullName(objTrans.parent) + "/" + objTrans.gameObject.name;
        }

        void refreshToolName()
        {
            IEnumerable<Tool> tools = tools_;
            
            while (tools.Count() > 0)
            {
                var searchedTools = tools.Where(x => x.originalName == tools.First().originalName);
                if (searchedTools.Count() > 1)
                {
                    int index = 0;
                    foreach (var tool in searchedTools)
                    {
                        if (tool.target != null && tool.target is Component)
                        {
                            tool.name = getFullName(((Component)tool.target).transform) + "/" + tool.target.GetType().Name + "." + tool.originalName;
                        }
                        else
                        {
                            tool.name = "/" + index + "/" + tool.originalName;
                            index++;
                        }
                    }
                }
                else
                {
                    searchedTools.First().name = searchedTools.First().originalName;
                }

                tools = tools.Where(x => x.originalName != tools.First().originalName).ToList();
            }

            isInsertedSystemMessage_ = false;
        }

        public void AddTool(ToolFunc tool)
        {
            if (tool == null)
            {
                return;
            }

            initTools();

            Tool newTool = new Tool();
            newTool.delegateFunc = tool;
            newTool.method = tool.Method;
            newTool.originalName = tool.Method.Name;
            var attrs = (SchemaNameAttribute[])newTool.method.GetCustomAttributes(typeof(SchemaNameAttribute), true);
            if (attrs.Length > 0)
            {
                newTool.originalName = attrs[0].Name;
            }
            newTool.name = newTool.originalName;

            tools_.Add(newTool);

            refreshToolName();
        }

        public void RemoveTool(ToolFunc tool)
        {
            if (tool == null)
            {
                return;
            }

            initTools();

            tools_ = tools_.Where(x => x.delegateFunc != tool).ToList();

            refreshToolName();
        }

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

                schemaData += generateTypeSchemaData(member.FieldType);
                var attrs = (SchemaDescriptionAttribute[])member.GetCustomAttributes(typeof(SchemaDescriptionAttribute));
                if (attrs.Length > 0)
                {
                    schemaData = schemaData.Remove(schemaData.Length - 1);
                    schemaData += ", 'description': '" + attrs[0].Content + "'}";
                }
                schemaData += "}";

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

            int toolsCount = tools_.Count;
            for (int toolsIndex = 0; toolsIndex < toolsCount; toolsIndex++)
            {
                string name = tools_[toolsIndex].name;

                var method = tools_[toolsIndex].method;
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

            int toolsCount = tools_.Count;
            for (int toolsIndex = 0; toolsIndex < toolsCount; toolsIndex++)
            {
                string name = tools_[toolsIndex].name;
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

                var method = tools_[toolsIndex].method;

                var argAttrs = (SchemaArgTypeAttribute[])method.GetCustomAttributes(typeof(SchemaArgTypeAttribute), true);
                if (argAttrs.Length > 0)
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

                tools_[toolsIndex].Call(data);
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
                if (trimedInput.Contains("<tool_call>"))
                {
                    notToolCall = false;
                }
                if ("<tool_call>".Contains(trimedInput))
                {
                    notToolCall = false;
                }

                if (notToolCall)
                {
                    callback(input);
                }
            }

            initTools();

            if (!isInsertedSystemMessage_ && chat[0].role == "system")
            {
                string content = chat[0].content;
                if (chat[0].content.Contains(SYSTEM_TEMPLATE_AFTER + "\n\n"))
                {
                    content = chat[0].content.Split(SYSTEM_TEMPLATE_AFTER + "\n\n")[1];
                }
                chat[0] = new ChatMessage { role = "system", content = SYSTEM_TEMPLATE_BEFORE + buildToolsSchema() + SYSTEM_TEMPLATE_AFTER + "\n\n" + content };
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

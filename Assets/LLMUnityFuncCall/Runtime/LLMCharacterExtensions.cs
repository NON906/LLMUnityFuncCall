using LLMUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMUnityFuncCall
{
    public static class LLMCharacterExtensions
    {
        const string SYSTEM_TEMPLATE_BEFORE = "\n\nYou are a helpful assistant that answers in JSON. Here's the json schema you must adhere to:\n<schema>\n";
        const string SYSTEM_TEMPLATE_AFTER = "\n</schema>";
        const string SYSTEM_TEMPLATE_EXAMPLE = "\nHere's an example of your output to:\n";

        static int countChar(string s, char c)
        {
            return s.Length - s.Replace(c.ToString(), "").Length;
        }

        static bool parseOutClass<T>(string rawTarget, out T targetT, out string target)
        {
            var targetList = rawTarget.Split("{").ToList();
            target = "{" + string.Join("{", targetList.GetRange(1, targetList.Count - 1));
            var targets = target.Split("}");
            target = targets[0];
            for (int loop = 1; loop < targets.Length; loop++)
            {
                target += "}";

                if (countChar(target, '{') == loop)
                {
                    try
                    {
                        targetT = JsonUtility.FromJson<T>(target);
                        if (targetT != null)
                        {
                            return true;
                        }
                    }
                    catch (ArgumentException)
                    {
                        targetT = default;
                    }
                }

                target += targets[loop];
            }

            targetT = default;
            return false;
        }

        static T parseOutClassStream<T>(string target)
        {
            if (countChar(target.Replace("\\\"", ""), '\"') % 2 == 1)
            {
                target += "\"";
            }

            int lastArray = target.LastIndexOf("[");
            int lastObject = target.LastIndexOf("{");
            string subTarget;
            string subLastTarget;
            if (lastArray < lastObject && lastObject >= 0)
            {
                subTarget = target.Substring(lastObject + 1);
                subLastTarget = target.Substring(0, lastObject + 1);
            }
            else if (lastArray > lastObject && lastArray >= 0)
            {
                subTarget = target.Substring(lastArray + 1);
                subLastTarget = target.Substring(0, lastArray + 1);
            }
            else
            {
                return default;
            }
            while (true)
            {
                lastArray = subLastTarget.LastIndexOf("[");
                if (lastArray < subLastTarget.LastIndexOf("]"))
                {
                    subLastTarget = subLastTarget.Substring(0, lastArray);
                    continue;
                }
                lastObject = subLastTarget.LastIndexOf("{");
                if (lastObject < subLastTarget.LastIndexOf("}"))
                {
                    subLastTarget = subLastTarget.Substring(0, lastObject);
                    continue;
                }
                int startArray = subTarget.IndexOf("]");
                int startObject = subTarget.IndexOf("}");
                if (startArray < 0 && startObject < 0)
                {
                    if (lastArray < lastObject && lastObject >= 0)
                    {
                        target += "}";
                        subLastTarget = subLastTarget.Substring(0, lastObject);
                    }
                    else if (lastArray > lastObject && lastArray >= 0)
                    {
                        target += "]";
                        subLastTarget = subLastTarget.Substring(0, lastArray);
                    }
                    else
                    {
                        break;
                    }
                }
                else if (startArray < startObject && startArray >= 0 && lastArray >= 0)
                {
                    subLastTarget = subLastTarget.Substring(0, lastArray);
                    subTarget = subTarget.Substring(startArray + 1);
                }
                else if (startArray > startObject && startObject >= 0 && lastObject >= 0)
                {
                    subLastTarget = subLastTarget.Substring(0, lastObject);
                    subTarget = subTarget.Substring(startObject + 1);
                }
                else
                {
                    break;
                }
            }

            T targetT;
            try
            {
                targetT = JsonUtility.FromJson<T>(target);
            }
            catch (ArgumentException)
            {
                targetT = default;
            }
            return targetT;
        }

        public static async Task<T> ChatFormat<T>(this LLMCharacter llmChara, string query, Callback<T> callback = null, EmptyCallback completionCallback = null, bool addToHistory = true, T example = null)
            where T : class
        {
            void parseCallback(string rawResult)
            {
                if (!rawResult.Contains("{"))
                {
                    return;
                }

                if (parseOutClass<T>(rawResult, out var targetT, out string target))
                {
                    if (callback != null)
                    {
                        callback(targetT);
                    }
                    llmChara.CancelRequests();
                    return;
                }

                if (callback != null)
                {
                    var targetT2 = parseOutClassStream<T>(target);
                    if (targetT2 != null)
                    {
                        callback(targetT2);
                    }
                }
            }

            if (llmChara.chat[0].role == "system")
            {
                string content = llmChara.chat[0].content;
                if (llmChara.chat[0].content.Contains(SYSTEM_TEMPLATE_BEFORE))
                {
                    content = llmChara.chat[0].content.Split(SYSTEM_TEMPLATE_BEFORE)[0];
                }
                content += SYSTEM_TEMPLATE_BEFORE + SchemaBuilder.Build<T>() + SYSTEM_TEMPLATE_AFTER;
                if (example != null)
                {
                    content += SYSTEM_TEMPLATE_EXAMPLE + JsonUtility.ToJson(example);
                }

                llmChara.chat[0] = new ChatMessage { role = "system", content = content };
            }

            string rawResult = await llmChara.Chat(query, parseCallback, completionCallback, addToHistory);
            if (rawResult == null)
            {
                return default;
            }

            if (parseOutClass(rawResult, out T targetT, out string target))
            {
                return targetT;
            }
            targetT = parseOutClassStream<T>(target);
            return targetT;
        }

        public static async Task<T> ChatFormatWithFuncCall<T>(this LLMCharacterFuncCall llmChara, string query, Callback<T> callback = null, EmptyCallback completionCallback = null, bool addToHistory = true, T example = null)
            where T : class
        {
            void parseCallback(string rawResult)
            {
                if (!rawResult.Contains("{"))
                {
                    return;
                }

                if (parseOutClass<T>(rawResult, out var targetT, out string target))
                {
                    if (callback != null)
                    {
                        callback(targetT);
                    }
                    llmChara.CancelRequests();
                    return;
                }

                if (callback != null)
                {
                    var targetT2 = parseOutClassStream<T>(target);
                    if (targetT2 != null)
                    {
                        callback(targetT2);
                    }
                }
            }

            if (llmChara.chat[0].role == "system")
            {
                string content = llmChara.chat[0].content;
                if (llmChara.chat[0].content.Contains(SYSTEM_TEMPLATE_BEFORE))
                {
                    content = llmChara.chat[0].content.Split(SYSTEM_TEMPLATE_BEFORE)[0];
                }
                content += SYSTEM_TEMPLATE_BEFORE + SchemaBuilder.Build<T>() + SYSTEM_TEMPLATE_AFTER;
                if (example != null)
                {
                    content += SYSTEM_TEMPLATE_EXAMPLE + JsonUtility.ToJson(example);
                }

                llmChara.chat[0] = new ChatMessage { role = "system", content = content };
            }

            string rawResult = await llmChara.ChatWithFuncCall(query, parseCallback, completionCallback, addToHistory);
            if (rawResult == null)
            {
                return default;
            }

            if (parseOutClass(rawResult, out T targetT, out string target))
            {
                return targetT;
            }
            targetT = parseOutClassStream<T>(target);

            return targetT;
        }
    }
}

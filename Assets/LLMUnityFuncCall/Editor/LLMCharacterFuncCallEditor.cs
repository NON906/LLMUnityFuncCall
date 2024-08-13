using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using LLMUnity;

namespace LLMUnityFuncCall
{
    [CustomEditor(typeof(LLMCharacterFuncCall))]
    public class LLMCharacterFuncCallEditor : LLMCharacterEditor
    {
        public override void OnInspectorGUI()
        {
            LLMCharacterFuncCall llmScript = (LLMCharacterFuncCall)target;
            SerializedObject llmScriptSO = new SerializedObject(llmScript);

            OnInspectorGUIStart(llmScriptSO);
            AddOptionsToggles(llmScriptSO);

            AddSetupSettings(llmScriptSO);
            AddChatSettings(llmScriptSO);
            Space();
            AddModelSettings(llmScriptSO, llmScript);

            Space();
            EditorGUILayout.LabelField("Func Call Settings", EditorStyles.boldLabel);
            llmScript.maxIterations = EditorGUILayout.IntField("Max Iterations", llmScript.maxIterations);
            EditorGUILayout.PropertyField(llmScriptSO.FindProperty("Tools"));

            OnInspectorGUIEnd(llmScriptSO);
        }
    }
}

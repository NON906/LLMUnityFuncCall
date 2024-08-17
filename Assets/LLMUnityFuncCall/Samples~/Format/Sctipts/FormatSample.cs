using LLMUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LLMUnityFuncCall.Sample
{
    public class FormatSample : MonoBehaviour
    {
        [Serializable]
        class CharacterJson
        {
            [SchemaRequired]
            public string name;

            [SchemaRequired]
            public string species;

            [SchemaRequired]
            public string role;

            public string[] personality_traits;

            public string[] special_attacks;
        }

        public LLMCharacter lLMCharacter;

        void streamFunc(CharacterJson content)
        {
            Debug.Log(JsonUtility.ToJson(content));
        }

        async void Start()
        {
            var charaJson = await lLMCharacter.ChatFormat<CharacterJson>("Please return a json object to represent Goku from the anime Dragon Ball Z?", streamFunc);

            Debug.Log(JsonUtility.ToJson(charaJson));
        }
    }
}

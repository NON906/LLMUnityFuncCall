using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnityFuncCall.Sample
{
    public class ChatButtonUI : MonoBehaviour
    {
        public InputField chatMessageInputField;
        public LLMCharacterFuncCall lLMCharacter;
        public ChatLog chatLog;
        public Button chatButton;
        public Button abortButton;

        async void Start()
        {
            await lLMCharacter.Warmup();
            chatButton.interactable = true;
        }


        public async void OnClickButton()
        {
            chatMessageInputField.interactable = false;
            chatButton.interactable = false;
            abortButton.interactable = true;

            string text = chatMessageInputField.text;
            chatLog.ChatStart(text);
            chatMessageInputField.text = "";
            await lLMCharacter.ChatWithFuncCall(text, chatLog.ChatCallback, chatComplete);

            if (abortButton != null)
            {
                abortButton.interactable = false;
            }
            if (chatButton != null)
            {
                chatButton.interactable = true;
            }
        }

        void chatComplete()
        {
            chatMessageInputField.interactable = true;
        }

        public void OnClickAbortButton()
        {
            lLMCharacter.CancelRequests();
            chatLog.ChatCansel();
        }
    }
}

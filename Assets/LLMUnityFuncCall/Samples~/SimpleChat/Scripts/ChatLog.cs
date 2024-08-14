using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnityFuncCall.Sample
{
    public class ChatLog : MonoBehaviour
    {
        public Transform targetTransform;
        public Text textBase;
        public Color userColor = Color.green;
        public Color aiColor = Color.black;

        Text userCurrentText_ = null;
        Text currentText_ = null;

        public void ChatStart(string userMessage)
        {
            userCurrentText_ = Instantiate(textBase, targetTransform);
            userCurrentText_.color = userColor;
            userCurrentText_.text = userMessage;
            userCurrentText_.gameObject.SetActive(true);

            currentText_ = Instantiate(textBase, targetTransform);
            currentText_.color = aiColor;
            currentText_.text = "";
            currentText_.gameObject.SetActive(true);
        }

        public void ChatCallback(string message)
        {
            if (currentText_ != null)
            {
                currentText_.text = message;
            }
        }

        public void ChatCansel()
        {
            if (userCurrentText_ != null)
            {
                Destroy(userCurrentText_.gameObject);
                userCurrentText_ = null;
            }
            if (currentText_ != null)
            {
                Destroy(currentText_.gameObject);
                currentText_ = null;
            }
        }
    }
}

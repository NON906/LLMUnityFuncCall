# LLMUnityFuncCall

[LLM for Unity](https://github.com/undreamai/LLMUnity)で[NousResearch/Hermes-2-Pro-Llama-3-8B-GGUF](https://huggingface.co/NousResearch/Hermes-2-Pro-Llama-3-8B-GGUF)による（Agentのような）Function Calling機能を使うためのアセットです。

## インストール方法

Package Managerの「+」ボタンで以下のgitリポジトリ（LLM for Unityとこのリポジトリ）を追加してください。

- ``https://github.com/undreamai/LLMUnity.git``
- ``https://github.com/NON906/LLMUnityFuncCall.git?path=Assets/LLMUnityFuncCall``

また、このリポジトリはモデルはHermes-2-Pro-Llama-3-8B-GGUF、Chat templateはchatmlのみ対応しています。  
LLMコンポーネントのインスペクターで以下のように設定してください。

- URL: ``https://huggingface.co/NousResearch/Hermes-2-Pro-Llama-3-8B-GGUF/resolve/main/Hermes-2-Pro-Llama-3-8B-Q4_K_M.gguf?download=true``
- Chat template: chatml

## 使用方法

工事中  
サンプルを確認してください
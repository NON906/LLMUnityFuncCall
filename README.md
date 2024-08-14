# LLMUnityFuncCall

（[日本語版はこちら](README_ja.md)）

This is assets for using Function Calling (like Agent) with [NousResearch/Hermes-2-Pro-Llama-3-8B-GGUF](https://huggingface.co/NousResearch/Hermes-2-Pro-Llama-3-8B-GGUF) in [LLM for Unity](https://github.com/undreamai/LLMUnity).

## How to install

Add the following git repositories (LLM for Unity and this repositories) using the "+" button in Package Manager.

- ``https://github.com/undreamai/LLMUnity.git``
- ``https://github.com/NON906/LLMUnityFuncCall.git?path=Assets/LLMUnityFuncCall``

This repository only supports the model Hermes-2-Pro-Llama-3-8B-GGUF and the chat template chatml.  
Please configure the following in the LLM component inspector.

- URL: ``https://huggingface.co/NousResearch/Hermes-2-Pro-Llama-3-8B-GGUF/resolve/main/Hermes-2-Pro-Llama-3-8B-Q4_K_M.gguf?download=true``
- Chat template: chatml

## How to use

WIP  
Please check the sample.
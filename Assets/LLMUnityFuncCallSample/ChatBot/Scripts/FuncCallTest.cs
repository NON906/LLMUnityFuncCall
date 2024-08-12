using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LLMUnityFuncCall.Sample
{
    public class FuncCallTest : MonoBehaviour
    {
        [Serializable]
        class GetCurrentStockPriceParameters
        {
            [SchemaRequired]
            public string symbol;
        }

        [SchemaArgType(typeof(GetCurrentStockPriceParameters))]
        [SchemaDescription("Get the current stock price for a given symbol.\nReturns:\n    The current stock price")]
        public void get_current_stock_price(FuncCallData data)
        {
            var castedArg = data.arguments as GetCurrentStockPriceParameters;
            data.output = "{'price': 200.0}";
        }
    }
}

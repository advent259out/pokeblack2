using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class BattleRuleSetContract
    {
        public string RuleSetId = "placeholder-rules";
        public string[] EnabledFormats = Array.Empty<string>();
    }
}


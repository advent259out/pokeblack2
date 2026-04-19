using System;

namespace PokeBlack2.Foundation.Runtime.Gen5.Contracts
{
    [Serializable]
    public sealed class ScriptInstructionContract
    {
        public int Offset = -1;
        public int Opcode = -1;
        public string Mnemonic = string.Empty;
        public int ByteLength = 0;
        public int[] Operands = Array.Empty<int>();
        public int BranchTargetOffset = -1;
    }
}

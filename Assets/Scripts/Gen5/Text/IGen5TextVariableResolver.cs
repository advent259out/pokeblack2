namespace PokeBlack2.Foundation.Runtime.Gen5.Text
{
    public interface IGen5TextVariableResolver
    {
        bool TryResolve(int controlCode, int[] arguments, out string value);
    }
}

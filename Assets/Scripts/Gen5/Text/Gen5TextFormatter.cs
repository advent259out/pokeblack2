using System;
using System.Collections.Generic;
using System.Text;
using PokeBlack2.Foundation.Runtime.Gen5.Contracts;

namespace PokeBlack2.Foundation.Runtime.Gen5.Text
{
    public static class Gen5TextFormatter
    {
        public static string FormatForDisplay(
            TextMessageContract message,
            IGen5TextVariableResolver variableResolver = null)
        {
            string[] pages = SplitIntoPages(message, variableResolver);
            return string.Join("\f", pages);
        }

        public static string[] SplitIntoPages(
            TextMessageContract message,
            IGen5TextVariableResolver variableResolver = null)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.Tokens == null || message.Tokens.Length == 0)
            {
                return new[] { message.Text ?? string.Empty };
            }

            List<string> pages = new List<string>();
            StringBuilder builder = new StringBuilder();
            foreach (TextTokenContract token in message.Tokens)
            {
                AppendToken(builder, pages, token, variableResolver);
            }

            pages.Add(builder.ToString());
            return pages.ToArray();
        }

        private static void AppendToken(
            StringBuilder builder,
            List<string> pages,
            TextTokenContract token,
            IGen5TextVariableResolver variableResolver)
        {
            if (token == null)
            {
                return;
            }

            switch (token.Kind ?? string.Empty)
            {
                case "text":
                    builder.Append(token.Text ?? string.Empty);
                    return;

                case "lineBreak":
                    builder.Append('\n');
                    return;

                case "pageBreak":
                    pages.Add(builder.ToString());
                    builder.Clear();
                    return;

                case "carriageReturn":
                    builder.Append('\r');
                    return;

                case "variable":
                    builder.Append(RenderVariable(token, variableResolver));
                    return;

                case "rawCodePoint":
                    builder.Append(RenderRawCodePoint(token.CodePoint));
                    return;

                default:
                    throw new InvalidOperationException($"Unsupported text token kind '{token.Kind}'.");
            }
        }

        private static string RenderVariable(
            TextTokenContract token,
            IGen5TextVariableResolver variableResolver)
        {
            int[] arguments = token.Arguments ?? Array.Empty<int>();
            if (variableResolver != null &&
                token.ControlCode >= 0 &&
                variableResolver.TryResolve(token.ControlCode, arguments, out string value))
            {
                return value ?? string.Empty;
            }

            List<string> values = new List<string>();
            if (token.ControlCode >= 0)
            {
                values.Add(token.ControlCode.ToString());
            }

            foreach (int argument in arguments)
            {
                values.Add(argument.ToString());
            }

            return $"VAR({string.Join(", ", values)})";
        }

        private static string RenderRawCodePoint(int codePoint)
        {
            return codePoint >= 0
                ? $"\\x{codePoint:X4}"
                : string.Empty;
        }
    }
}

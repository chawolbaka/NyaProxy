using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsolePlus.InteractiveMode
{
    public struct Token
    {
        public readonly SyntaxKind Kind;
        public readonly string Value;

        public Token(SyntaxKind kind)
        {
            Kind = kind;
            Value = null;
        }

        public Token(SyntaxKind kind, string value)
        {
            Kind = kind;
            Value = value;
        }

        public static List<Token> Parse(string text)
        {
            List<Token> tokens = new List<Token>();
            StringBuilder arg = new StringBuilder();
            tokens.Add(new Token(SyntaxKind.StartToken));
            SyntaxKind? LastDoubleQuotes = null;
            text = text.TrimStart(' ');

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    if (tokens.Last().Kind == SyntaxKind.StartDoubleQuotesToken)
                    {
                        arg.Append(text[i]);
                        continue;
                    }
                    if (arg.Length > 0)
                    {
                        tokens.Add(new Token(SyntaxKind.StringToken, arg.ToString()));
                        arg.Clear();
                    }
                    if (tokens.Last().Kind != SyntaxKind.SplitToken)
                    {
                        tokens.Add(new Token(SyntaxKind.SplitToken));
                    }
                }
                else if (text[i] == '\"')
                {
                    if (arg.Length > 0)
                    {
                        tokens.Add(new Token(SyntaxKind.StringToken, arg.ToString()));
                        arg.Clear();
                    }
                    if (LastDoubleQuotes.HasValue)
                    {
                        if (LastDoubleQuotes.Value == SyntaxKind.StartDoubleQuotesToken)
                            tokens.Add(new Token(SyntaxKind.EndDoubleQuotesToken, "\""));
                        else
                            tokens.Add(new Token(SyntaxKind.StartDoubleQuotesToken, "\""));

                        LastDoubleQuotes = tokens.Last().Kind;
                    }
                    else
                    {
                        LastDoubleQuotes = SyntaxKind.StartDoubleQuotesToken;
                        tokens.Add(new Token(SyntaxKind.StartDoubleQuotesToken, "\""));
                    }
                }
                else if (text[i] == '\\' && i + 1 < text.Length && text[i + 1] is '\"')
                {
                    arg.Append(text[++i]);
                }
                else
                {
                    arg.Append(text[i]);
                }
            }

            if (arg.Length > 0)
                tokens.Add(new Token(SyntaxKind.StringToken, arg.ToString()));
         
            if (!LastDoubleQuotes.HasValue || LastDoubleQuotes.Value != SyntaxKind.StartDoubleQuotesToken)
                tokens.Add(new Token(SyntaxKind.EndToken));

            return tokens;
        }
    }
}

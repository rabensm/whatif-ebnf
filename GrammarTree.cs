#nullable enable

using System.IO;
using System.Text.RegularExpressions;

public partial class GrammarTree
{
    public GrammarTree(string grammarFilePath)
    {
        FileInfo grammarFile = new(grammarFilePath);
        string grammar = File.ReadAllText(grammarFile.FullName);
        GrammarParser gp = new(grammar);
        gp.Parse();
    }

    private partial class GrammarParser(string grammar)
    {
        private readonly string grammar = grammar;
        private int index = 0;
        [GeneratedRegex(@"[a-zA-Z][a-zA-Z0-9_]*")]
        private static partial Regex NonTerminalRE();

        public void Parse()
        {
            SkipWhitespace();
            while (index < grammar.Length)
            {
                ParseRule();
            }
        }

        private string Next()
        {
            return grammar[index..];
        }

        private char NextChar()
        {
            return grammar[index];
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(NextChar()))
            {
                index++;
            }
        }

        private void BumpIndex(int n)
        {
            index += n;
            SkipWhitespace();
        }

        private bool TryMatch(string s)
        {
            if (Next().StartsWith(s))
            {
                BumpIndex(s.Length);
                return true;
            }
            return false;
        }

        private void Match(string s)
        {
            if (!TryMatch(s))
            {
                throw new System.Exception($"Expected '{s}'");
            }
        }

        private void ParseRule()
        {
            string RuleLHSName = ParseNonTerminal();
            Match("::=");
            while (index < grammar.Length)
            {
            }
        }

        private string? TryNonTerminal()
        {
            Match m = NonTerminalRE().Match(Next());
            if (m.Success)
            {
                BumpIndex(m.Length);
                return m.Value;
            }
            return null;
        }

        private string ParseNonTerminal()
        {
            string? nt = TryNonTerminal() ?? throw new System.Exception("Expected non-terminal");
            return nt;
        }
    }
}
#nullable enable

using System.Text.RegularExpressions;
using static Grammar.GrammarGraph;

namespace Grammar
{
    public partial class GrammarParser(string grammar, GrammarGraph ggraph)
    {
        private readonly string grammar = grammar;
        private readonly GrammarGraph ggraph = ggraph;
        private int index = 0;
        [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_]*")]
        private static partial Regex NonTerminalRE();

        private struct BracketPair
        {
            public NodeType Type;
            public string Open;
            public string Close;
        }

        private static readonly BracketPair[] BracketPairs =
        [
            new BracketPair { Type = NodeType.Grouping, Open = "(", Close = ")" },
            new BracketPair { Type = NodeType.NoneOrMore, Open = "{", Close = "}" },
            new BracketPair { Type = NodeType.NoneOrOnce, Open = "[", Close = "]" }
        ];

        public void Parse()
        {
            SkipWhitespace();
            while (index < grammar.Length)
                ParseRule();
        }

        private GrammarGraph.Node GetNonTerminal(string name)
        {
            if (!ggraph.NonTerminalsMap.TryGetValue(name, out Node? value))
            {
                value = new Node(NodeType.NonTerminal, name);
                ggraph.NonTerminalsMap[name] = value;
            }
            return value;
        }

        private string Next() { return grammar[index..]; }

        private char NextChar() { return grammar[index]; }

        private void SkipWhitespace()
        {
            while (index < grammar.Length && char.IsWhiteSpace(NextChar()))
                index++;
        }

        private void BumpIndex(int n)
        {
            index += n;
            SkipWhitespace();
        }

        private bool CanMatch(string s) { return Next().StartsWith(s); }

        private bool TryMatch(string s)
        {
            if (CanMatch(s))
            {
                BumpIndex(s.Length);
                return true;
            }
            return false;
        }

        private void Match(string s)
        {
            if (!TryMatch(s))
                throw new System.Exception($"Expected '{s}'");
        }

        private void ParseRule()
        {
            GrammarGraph.Node ruleNode = GetNonTerminal(ParseNonTerminal());
            ggraph.EXPRNode.Children[0].AddChild(ruleNode);
            Match("::=");
            ParseIntoNode(ruleNode);
        }

        private void ParseIntoNode(Node node)
        {
            Node? nextNode;

            while (true)
            {
                nextNode = null;

                string? nt = TryNonTerminal();
                if (nt != null)
                    nextNode = GetNonTerminal(nt);

                if (nextNode == null)
                {
                    string? term = TryTerminal();
                    if (term != null)
                        nextNode = new Node(NodeType.Terminal, term);
                }

                if (nextNode == null)
                {
                    string? reg = TryRegEx();
                    if (reg != null)
                        nextNode = new Node(NodeType.RegEx, reg);
                }

                if (nextNode == null)
                {
                    foreach (BracketPair bp in BracketPairs)
                    {
                        if (TryMatch(bp.Open))
                        {
                            Node groupNode = new(bp.Type);
                            ParseIntoNode(groupNode);
                            nextNode = groupNode;
                            break;
                        }
                    }
                }

                if (nextNode == null)
                    throw new System.Exception("Expected non-terminal, terminal, regex, or grouping");

                if (node.Type != NodeType.Concatenate && TryMatch(","))
                {
                    Node concatNode = new(NodeType.Concatenate);
                    concatNode.AddChild(nextNode);
                    ParseIntoNode(concatNode);
                    nextNode = concatNode;
                }
                if (node.Type != NodeType.Alternate && TryMatch("|"))
                {
                    Node altNode = new(NodeType.Alternate);
                    altNode.AddChild(nextNode);
                    ParseIntoNode(altNode);
                    nextNode = altNode;
                }
                node.AddChild(nextNode);

                BracketPair? nodeBracket = null;
                BracketPair? closeBracket = null;
                foreach (BracketPair bp in BracketPairs)
                {
                    if (bp.Type == node.Type)
                        nodeBracket = bp;
                    if (CanMatch(bp.Close))
                        closeBracket = bp;
                }
                if (closeBracket != null)
                {
                    if (nodeBracket != null)
                    {
                        if (nodeBracket.Value.Type == closeBracket.Value.Type)
                            Match(closeBracket.Value.Close);
                        else
                            throw new System.Exception("Mismatched brackets");
                    }
                    return;
                }

                if (CanMatch(";"))
                {
                    if (node.Type == NodeType.NonTerminal)
                        Match(";");
                    return;
                }

                if (node.Type == NodeType.Concatenate)
                    Match(",");

                if (node.Type == NodeType.Alternate)
                    Match("|");
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

        private string? TryTerminal()
        {
            if (NextChar() == '"' || NextChar() == '\'')
            {
                char quote = NextChar();
                index++;
                string val = "";
                while (true)
                {
                    if (Next() == "\\" + quote)
                    {
                        val += quote;
                        index += 2;
                        continue;
                    }
                    if (NextChar() == quote)
                    {
                        BumpIndex(1);
                        return val;
                    }
                    val += NextChar();
                    index++;
                }
            }
            return null;
        }

        private string? TryRegEx()
        {
            if (NextChar() == '/')
            {
                index++;
                string val = "";
                while (true)
                {
                    if (Next() == "\\/")
                    {
                        val += "\\/";
                        index += 2;
                        continue;
                    }
                    if (NextChar() == '/')
                    {
                        BumpIndex(1);
                        return val;
                    }
                    val += NextChar();
                    index++;
                }
            }
            return null;
        }
    }
}
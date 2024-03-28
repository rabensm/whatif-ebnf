#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Grammar
{
    public class GrammarTree
    {
        public readonly Dictionary<string, Node> NonTerminalsMap = [];
        public readonly Node EXPRNode = new(NodeType.NonTerminal, "EXPR");
        private string Input = "";

        public GrammarTree()
        {
            EXPRNode.AddChild(new Node(NodeType.Alternate));
            NonTerminalsMap["EXPR"] = EXPRNode;
            FileInfo grammarFile = new("wif.ebnf");
            string grammar = File.ReadAllText(grammarFile.FullName);
            GrammarParser gp = new(grammar, this);
            gp.Parse();
        }

        public void Parse(string input)
        {
            Input = input;
            int index = 0;
            SkipWhitespace(ref index);
            MatchNode(EXPRNode, ref index);
        }

        private void SkipWhitespace(ref int index)
        {
            while (index < Input.Length && char.IsWhiteSpace(Input[index]))
                index++;
        }

        private void BumpIndex(ref int index, int n)
        {
            index += n;
            SkipWhitespace(ref index);
        }

        private bool MatchNode(Node node, ref int index)
        {
            switch (node.Type)
            {
                case NodeType.NonTerminal:
                    return MatchNonTerminal(node, ref index);
                case NodeType.Terminal:
                    return MatchTerminal(node, ref index);
                case NodeType.RegEx:
                    return MatchRegEx(node, ref index);
                case NodeType.Concatenate:
                    return MatchConcatenate(node, ref index);
                case NodeType.Alternate:
                    return MatchAlternate(node, ref index);
                case NodeType.NoneOrOnce:
                    return MatchNoneOrOnce(node, ref index);
                case NodeType.NoneOrMore:
                    return MatchNoneOrMore(node, ref index);
                case NodeType.Grouping:
                    return MatchGrouping(node, ref index);
                default:
                    return false;
            }
        }

        private bool MatchNonTerminal(Node node, ref int index)
        {
            return MatchNode(node.Children[0], ref index);
        }

        private bool MatchTerminal(Node node, ref int index)
        {
            if (Input[index..].StartsWith(node.Value))
            {
                BumpIndex(ref index, node.Value.Length);
                return true;
            }
            return false;
        }

        private bool MatchRegEx(Node node, ref int index)
        {
            Regex regex = new(node.Value);
            Match match = regex.Match(Input[index..]);
            if (match.Success)
            {
                BumpIndex(ref index, match.Length);
                return true;
            }
            return false;
        }

        private bool MatchConcatenate(Node node, ref int index)
        {
            int oldIndex = index;
            foreach (Node child in node.Children)
                if (!MatchNode(child, ref index))
                {
                    index = oldIndex;
                    return false;
                }
            return true;
        }

        private bool MatchAlternate(Node node, ref int index)
        {
            int tempIndex;
            foreach (Node child in node.Children)
            {
                tempIndex = index;
                if (MatchNode(child, ref tempIndex))
                {
                    index = tempIndex;
                    return true;
                }
            }
            return false;
        }

        private bool MatchNoneOrOnce(Node node, ref int index)
        {
            MatchNode(node.Children[0], ref index);
            return true;
        }

        private bool MatchNoneOrMore(Node node, ref int index)
        {
            while (MatchNode(node.Children[0], ref index));
            return true;
        }

        private bool MatchGrouping(Node node, ref int index)
        {
            return MatchNode(node.Children[0], ref index);
        }

        public enum NodeType
        {
            NonTerminal, Terminal, RegEx, Concatenate, Alternate, NoneOrOnce, NoneOrMore, Grouping
        }

        public class Node(NodeType type, string value = "")
        {
            public NodeType Type = type;
            public string Value = value;
            public List<Node> Children = [];
            public void AddChild(Node child)
            {
                Children.Add(child);
            }
        }
    }
}
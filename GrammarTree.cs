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

        }

        public enum NodeType
        {
            NonTerminal, Terminal, RegEx, Concatenate, Alternate, NoneOrOnce, NoneOrMore, Grouping
        }

        public class Node(NodeType type, string? value = null)
        {
            public NodeType Type = type;
            public string? Value = value;
            public List<Node> Children = [];
            public void AddChild(Node child)
            {
                Children.Add(child);
            }
        }
    }
}
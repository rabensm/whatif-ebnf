#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;

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

        private string VisualizeTree()
        {

            var json = new
            {
                root = new Dictionary<string, object>
                {
                    ["items"] = new[]
                    {
                        new { text = "root" }
                    },
                    ["children"] = new List<object>()
                },
                kind = new { tree = true }
            };

            List<object> nodes = (List<object>)json.root["children"];

            AddNode(EXPRNode, nodes, true);

            return JsonSerializer.Serialize(json);

            void AddNode(Node node, List<object> nodes, bool addNonTermChildren)
            {
                List<object> children = [];

                if ((node.Type != NodeType.NonTerminal) || addNonTermChildren)
                {
                    if (node.Type == NodeType.NonTerminal && node != EXPRNode)
                        addNonTermChildren = false;
                    foreach (Node child in node.Children)
                    {
                        AddNode(child, children, addNonTermChildren);
                    }
                }

                nodes.Add(new Dictionary<string, object>
                {
                    ["items"] = new[]
                    {
                        new { text = node.Type.ToString(), emphasis = "style1"},
                        node.Type == NodeType.NonTerminal ?
                            new { text = node.Value, emphasis = "style2"} :
                            node.Value == "" ?
                                new { text = "", emphasis = ""} :
                                new { text = " \"" + node.Value + "\"", emphasis = ""}
                    },
                    ["children"] = children
                });
            }
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
            int indexIn = index;
            bool matched;

            matched = node.Type switch
            {
                NodeType.NonTerminal => MatchNonTerminal(node, ref index),
                NodeType.Terminal => MatchTerminal(node, ref index),
                NodeType.RegEx => MatchRegEx(node, ref index),
                NodeType.Concatenate => MatchConcatenate(node, ref index),
                NodeType.Alternate => MatchAlternate(node, ref index),
                NodeType.NoneOrOnce => MatchNoneOrOnce(node, ref index),
                NodeType.NoneOrMore => MatchNoneOrMore(node, ref index),
                NodeType.Grouping => MatchGrouping(node, ref index),
                _ => false,
            };

            if (matched)
            {
                Debug.WriteLine($"Matched {node.Type} '{node.Value}' to {Input[indexIn..index]}");
            }

            return matched;
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
            while (MatchNode(node.Children[0], ref index)) ;
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
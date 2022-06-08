using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace SVGGenerate
{
    internal static class ParameterNodeHelper
    {
        public static void Handle(SVGGenerateModel model)
        {
            var parameterNode = model.Root.SelectSingleNode($"//{model.Manager.GetNamespacesInScope(XmlNamespaceScope.Local).ToList().FirstOrDefault(it => it.Key.ToLower().Contains("cpdts")).Key}:parameters", model.Manager);
            if (parameterNode != null)
            {
                UpdateParametersNode(parameterNode, model);
            }
        }

        private static void UpdateParametersNode(XmlNode parameterNode, SVGGenerateModel model)
        {
            var nodeList = parameterNode.ChildNodes.Cast<XmlNode>().ToList();
            var tmpNodes = GetParameterNodes(nodeList, model.TextElementDict);
            parameterNode.RemoveAll();
            tmpNodes.ForEach(it => parameterNode.AppendChild(it));
        }

        private static List<XmlNode> GetParameterNodes(List<XmlNode> nodeList, Dictionary<string, string> textElementDict)
        {
            var result = new List<XmlNode>();

            // i1
            var image_i_pattern = @"^i\d+$";
            var image_i_nodes = new List<XmlNode>();

            // i1t
            var image_i_t_pattern = @"^i\d+t$";
            var image_i_t_nodes = new List<XmlNode>();

            // text_elementText
            var text_elementText_nodes = new List<XmlNode>();

            // t1t
            var text_t_t_pattern = @"^t\d+t$";
            var text_t_t_nodes = new List<XmlNode>();

            // t1f
            var text_t_f_pattern = @"^t\d+f$";
            var text_t_f_nodes = new List<XmlNode>();

            // t1ff
            var text_t_f_f_pattern = @"^t\d+ff$";
            var text_t_f_f_nodes = new List<XmlNode>();

            foreach (var node in nodeList)
            {
                foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (Regex.Match(attribute.Value, image_i_pattern).Success)
                    {
                        image_i_nodes.Add(node);
                        break;
                    }
                    if (Regex.Match(attribute.Value, image_i_t_pattern).Success)
                    {
                        image_i_t_nodes.Add(node);
                        break;
                    }
                    if (attribute.Value == "elementText")
                    {
                        text_elementText_nodes.Add(node);
                        break;
                    }
                    if (Regex.Match(attribute.Value, text_t_t_pattern).Success)
                    {
                        text_t_t_nodes.Add(node);
                        break;
                    }
                    if (Regex.Match(attribute.Value, text_t_f_pattern).Success)
                    {
                        text_t_f_nodes.Add(node);
                        break;
                    }
                    if (Regex.Match(attribute.Value, text_t_f_f_pattern).Success)
                    {
                        text_t_f_f_nodes.Add(node);
                        break;
                    }
                }
            }

            image_i_nodes = image_i_nodes.Count > 0 ? GenerateParameterNodes(image_i_nodes, textElementDict) : image_i_nodes;
            image_i_t_nodes = image_i_t_nodes.Count > 0 ? GenerateParameterNodes(image_i_t_nodes, textElementDict) : image_i_t_nodes;
            text_elementText_nodes = text_elementText_nodes.Count > 0 ? GenerateParameterNodes(text_elementText_nodes, textElementDict, true) : text_elementText_nodes;
            text_t_t_nodes = text_t_t_nodes.Count > 0 ? GenerateParameterNodes(text_t_t_nodes, textElementDict) : text_t_t_nodes;
            text_t_f_nodes = text_t_f_nodes.Count > 0 ? GenerateParameterNodes(text_t_f_nodes, textElementDict) : text_t_f_nodes;
            text_t_f_f_nodes = text_t_f_f_nodes.Count > 0 ? GenerateParameterNodes(text_t_f_f_nodes, textElementDict) : text_t_f_f_nodes;

            result.AddRange(image_i_nodes);
            result.AddRange(image_i_t_nodes);
            result.AddRange(text_elementText_nodes);
            result.AddRange(text_t_t_nodes);
            result.AddRange(text_t_f_nodes);
            result.AddRange(text_t_f_f_nodes);

            return result;
        }

        private static List<XmlNode> GenerateParameterNodes(List<XmlNode> nodes, Dictionary<string, string> textElementDict, bool isTextElement = false)
        {
            var number_parttern = @"\d+";
            var tmpNodes = new List<XmlNode>();
            foreach (var node in nodes)
            {
                var coypNode = node.CloneNode(true);
                var name = node.Attributes["name"].Value;
                var target = node.Attributes["target"].Value;
                var n = Convert.ToInt32(Regex.Match(target, number_parttern).Groups[0].Value);
                node.Attributes["target"].Value = target.Replace(n.ToString(), (2 * n - 1).ToString());
                coypNode.Attributes["target"].Value = target.Replace(n.ToString(), (2 * n).ToString());
                if (isTextElement)
                {
                    node.Attributes["name"].Value = $"{name}-{2 * n - 1}";
                    coypNode.Attributes["name"].Value = $"{name}-{2 * n }";
                    textElementDict.Add(node.Attributes["target"].Value, node.Attributes["name"].Value);
                    textElementDict.Add(coypNode.Attributes["target"].Value, coypNode.Attributes["name"].Value);
                }
                else
                {
                    node.Attributes["name"].Value = name.Replace(n.ToString(), (2 * n - 1).ToString());
                    coypNode.Attributes["name"].Value = name.Replace(n.ToString(), (2 * n).ToString());
                }

                tmpNodes.Add(node);
                tmpNodes.Add(coypNode);
            }
            return tmpNodes;
        }
    }
}
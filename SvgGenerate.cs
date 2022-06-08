using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace SingleToDuplciate
{
    public class SvgGenerate
    {
        private XmlDocument _xmlDoc;
        private XmlNamespaceManager _manager;
        private int _svgWidth = 1000;
        private int _svgHeight = 361;
        private int _originalWidth;
        private int _originalHeight;
        private double _sizeScale;
        private double _offset1;
        private double _offset2;
        private Dictionary<string, string> _textElementDict = new Dictionary<string, string>();
        private Dictionary<string, string> _defsRectIdDict = new Dictionary<string, string>();
        private Dictionary<string, string> _clipPathRectIdDict = new Dictionary<string, string>();
        private XmlElement _originalContainer;
        private XmlElement _duplicateContainer;

        public SvgGenerate(Stream stream, int svgWidth, int svgHeight)
        {
            if (stream.Position > 0)
            {
                stream.Position = 0;
            }
            _xmlDoc = new XmlDocument();
            _xmlDoc.Load(stream);
            _manager = GetNamespaceManager(_xmlDoc);
            _svgWidth = svgWidth;
            _svgHeight = svgHeight;
        }

        public MemoryStream GenerateDuplicateIamgeStream()
        {
            var outStream = new MemoryStream();
            var root = _xmlDoc.DocumentElement;

            UpdateSvgNode(root);

            AddContainerNode(root);

            var parameterNode = root.SelectSingleNode($"//{_manager.GetNamespacesInScope(XmlNamespaceScope.Local).ToList().FirstOrDefault(it => it.Key.ToLower().Contains("cpdts")).Key}:parameters", _manager);
            if (parameterNode != null)
            {
                UpdateParametersNode(parameterNode);
            }

            var gFillPathNode = root.SelectSingleNode("//svg:g[@fill]", _manager);
            if (gFillPathNode != null)
            {
                UpdateGFillPath(gFillPathNode);
            }

            var defsNodes = root.SelectNodes("/svg:defs", _manager);
            if (defsNodes?.Count > 0)
            {
                UpdateDefsNode(defsNodes.Cast<XmlNode>().ToList());
            }

            var clipPathNodes = root.SelectNodes("/svg:clipPath", _manager);
            if (clipPathNodes?.Count > 0)
            {
                UpdateClipPathNode(defsNodes.Cast<XmlNode>().ToList());
            }

            var gImageNodes = root.SelectNodes("//*[@cpbv:class='image']", _manager);
            if (gImageNodes?.Count > 0)
            {
                UpdateGImageNodes(gImageNodes.Cast<XmlNode>().ToList());
            }

            var gTextNodes = root.SelectNodes("//*[@cpbv:class='text']", _manager);

            if (gTextNodes?.Count > 0)
            {
                UpdateGTextNodes(gTextNodes.Cast<XmlNode>().ToList());
            }

            AddMaskLayer(root);

            _xmlDoc.Save(outStream);
            outStream.Position = 0;
            return outStream;
        }

        private void UpdateParametersNode(XmlNode node)
        {
            var nodeList = node.ChildNodes.Cast<XmlNode>().ToList();
            var tmpNodes = GetParameterNodes(nodeList);
            node.RemoveAll();
            tmpNodes.ForEach(it => node.AppendChild(it));
        }

        private void UpdateSvgNode(XmlNode node)
        {
            _originalHeight = decimal.ToInt32(Convert.ToDecimal(node.Attributes["height"].Value));
            _originalWidth = decimal.ToInt32(Convert.ToDecimal(node.Attributes["width"].Value));

            node.Attributes["width"].Value = _svgWidth.ToString();
            node.Attributes["height"].Value = _svgHeight.ToString();
            if (node.Attributes["viewBox"] != null)
            {
                node.Attributes["viewBox"].Value = $"0 0 {_svgWidth} {_svgHeight}";
            }

            var heightScale = _svgHeight * 1.0 / _originalHeight * 1.0;
            var widthScale = (_svgWidth / 2) * 1.0 / _originalWidth * 1.0;
            _sizeScale = heightScale < widthScale ? heightScale : widthScale;
            _offset1 = _svgWidth / 2;
            _offset2 = (_svgWidth / 2 - _sizeScale * _originalWidth) / 2;
        }

        private void UpdateGImageNodes(List<XmlNode> nodes)
        {
            var parentNode = nodes[0].ParentNode;
            foreach (var node in nodes)
            {
                var index = nodes.IndexOf(node);
                var orginalNode = GenerateImageNode(node, true, index);
                var duplicateNode = GenerateImageNode(node, false, index);

                parentNode.RemoveChild(node);
                _originalContainer.AppendChild(orginalNode);
                _duplicateContainer.AppendChild(duplicateNode);
            }
        }

        private void UpdateGTextNodes(List<XmlNode> nodes)
        {
            var parentNode = nodes[0].ParentNode;
            foreach (var node in nodes)
            {
                var index = nodes.IndexOf(node);

                var orginalNode = GenerateTextNode(node, true, index);
                var duplicateNode = GenerateTextNode(node, false, index);

                parentNode.RemoveChild(node);
                _originalContainer.AppendChild(orginalNode);
                _duplicateContainer.AppendChild(duplicateNode);
            }
        }

        private void UpdateGFillPath(XmlNode node)
        {
            var parentNode = node.ParentNode;

            var orginalNode = GenerateGFillPathNode(node, true);
            var duplicateNode = GenerateGFillPathNode(node, false);

            parentNode.RemoveChild(node);
            _originalContainer.AppendChild(orginalNode);
            _duplicateContainer.AppendChild(duplicateNode);
        }

        private void UpdateDefsNode(List<XmlNode> nodes)
        {
            var parentNode = nodes[0].ParentNode;
            foreach (var node in nodes)
            {
                var index = nodes.IndexOf(node);
                var orginalNode = GenerateDefsNode(node, true);
                var duplicateNode = GenerateDefsNode(node, false);

                parentNode.RemoveChild(node);
                _originalContainer.AppendChild(orginalNode);
                _duplicateContainer.AppendChild(duplicateNode);
            }
        }

        private void UpdateClipPathNode(List<XmlNode> nodes)
        {
            var parentNode = nodes[0].ParentNode;
            foreach (var node in nodes)
            {
                var index = nodes.IndexOf(node);
                var orginalNode = GenerateClipPathNode(node, true);
                var duplicateNode = GenerateClipPathNode(node, false);

                parentNode.RemoveChild(node);
                _originalContainer.AppendChild(orginalNode);
                _duplicateContainer.AppendChild(duplicateNode);
            }
        }

        private List<XmlNode> GetParameterNodes(List<XmlNode> nodeList)
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

            image_i_nodes = image_i_nodes.Count > 0 ? GenerateParameterNodes(image_i_nodes) : image_i_nodes;
            image_i_t_nodes = image_i_t_nodes.Count > 0 ? GenerateParameterNodes(image_i_t_nodes) : image_i_t_nodes;
            text_elementText_nodes = text_elementText_nodes.Count > 0 ? GenerateParameterNodes(text_elementText_nodes, true) : text_elementText_nodes;
            text_t_t_nodes = text_t_t_nodes.Count > 0 ? GenerateParameterNodes(text_t_t_nodes) : text_t_t_nodes;
            text_t_f_nodes = text_t_f_nodes.Count > 0 ? GenerateParameterNodes(text_t_f_nodes) : text_t_f_nodes;
            text_t_f_f_nodes = text_t_f_f_nodes.Count > 0 ? GenerateParameterNodes(text_t_f_f_nodes) : text_t_f_f_nodes;

            result.AddRange(image_i_nodes);
            result.AddRange(image_i_t_nodes);
            result.AddRange(text_elementText_nodes);
            result.AddRange(text_t_t_nodes);
            result.AddRange(text_t_f_nodes);
            result.AddRange(text_t_f_f_nodes);

            return result;
        }

        private XmlNode GenerateImageNode(XmlNode node, bool isOriginal, int index)
        {
            var resultNode = node.CloneNode(true);

            var transform = resultNode.Attributes["transform"].Value;
            var n1 = transform.IndexOf('(');
            var n2 = transform.IndexOf(')');
            var transformStr = transform.Substring(n1 + 1, n2 - n1 - 1);
            var transformArray = transformStr.Contains(',') ? transformStr.Split(",") : transformStr.Split(" ");
            transformArray[0] = (float.Parse(transformArray[0]) * _sizeScale).ToString("f2");
            transformArray[1] = (float.Parse(transformArray[1]) * _sizeScale).ToString("f2");
            transformArray[2] = (float.Parse(transformArray[2]) * _sizeScale).ToString("f2");
            transformArray[3] = (float.Parse(transformArray[3]) * _sizeScale).ToString("f2");
            transformArray[4] = (float.Parse(transformArray[4]) * _sizeScale + (isOriginal ? _offset1 : 0) + _offset2).ToString("f2");
            transformArray[5] = (float.Parse(transformArray[5]) * _sizeScale).ToString("f2");
            var duplicateTransformStr = string.Join(" ", transformArray);
            resultNode.Attributes["transform"].Value = $"matrix({duplicateTransformStr})";

            UpdateImageNodeId(resultNode, isOriginal ? index : index + 1);
            return resultNode;
        }

        private XmlNode GenerateTextNode(XmlNode node, bool isOriginal, int index)
        {
            var resultNode = node.CloneNode(true);
            var transform = resultNode.Attributes["transform"].Value;
            var n1 = transform.IndexOf('(');
            var n2 = transform.IndexOf(')');
            var transformStr = transform.Substring(n1 + 1, n2 - n1 - 1);
            var transformArray = transformStr.Contains(',') ? transformStr.Split(",") : transformStr.Split(" ");
            transformArray[0] = (float.Parse(transformArray[0]) * _sizeScale).ToString("f2");
            transformArray[1] = (float.Parse(transformArray[1]) * _sizeScale).ToString("f2");
            transformArray[2] = (float.Parse(transformArray[2]) * _sizeScale).ToString("f2");
            transformArray[3] = (float.Parse(transformArray[3]) * _sizeScale).ToString("f2");
            transformArray[4] = (float.Parse(transformArray[4]) * _sizeScale + (isOriginal ? _offset1 : 0) + _offset2).ToString("f2");
            transformArray[5] = (float.Parse(transformArray[5]) * _sizeScale).ToString("f2");
            var duplicateTransformStr = string.Join(" ", transformArray);
            resultNode.Attributes["transform"].Value = $"matrix({duplicateTransformStr})";

            UpdateTextNodeId(resultNode, isOriginal, _sizeScale);

            return resultNode;
        }

        private List<XmlNode> GenerateParameterNodes(List<XmlNode> nodes, bool isTextElement = false)
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
                    _textElementDict.Add(node.Attributes["target"].Value, node.Attributes["name"].Value);
                    _textElementDict.Add(coypNode.Attributes["target"].Value, coypNode.Attributes["name"].Value);
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

        private XmlNode GenerateGFillPathNode(XmlNode node, bool isOriginal)
        {
            var resultNode = node.CloneNode(true);

            var transform = resultNode.Attributes["transform"].Value;
            var n1 = transform.IndexOf('(');
            var n2 = transform.IndexOf(')');
            var transformStr = transform.Substring(n1 + 1, n2 - n1 - 1);
            var transformArray = transformStr.Contains(',') ? transformStr.Split(",") : transformStr.Split(" ");
            transformArray[0] = (float.Parse(transformArray[0]) * _sizeScale).ToString("f2");
            transformArray[1] = (float.Parse(transformArray[1]) * _sizeScale).ToString("f2");
            transformArray[2] = (float.Parse(transformArray[2]) * _sizeScale).ToString("f2");
            transformArray[3] = (float.Parse(transformArray[3]) * _sizeScale).ToString("f2");
            transformArray[4] = (float.Parse(transformArray[4]) * _sizeScale + (isOriginal ? _offset1 : 0) + _offset2).ToString("f2");
            transformArray[5] = (float.Parse(transformArray[5]) * _sizeScale).ToString("f2");
            var duplicateTransformStr = string.Join(" ", transformArray);
            resultNode.Attributes["transform"].Value = $"matrix({duplicateTransformStr})";

            return resultNode;
        }

        private XmlNode GenerateDefsNode(XmlNode node, bool isOriginal)
        {
            var resultNode = node.CloneNode(true);

            var gNode = resultNode.SelectSingleNode("//svg:g", _manager);
            {
                var transform = gNode.Attributes["transform"].Value;
                var n1 = transform.IndexOf('(');
                var n2 = transform.IndexOf(')');
                var transformStr = transform.Substring(n1 + 1, n2 - n1 - 1);
                var transformArray = transformStr.Contains(',') ? transformStr.Split(",") : transformStr.Split(" ");
                transformArray[0] = (float.Parse(transformArray[0]) * _sizeScale).ToString("f2");
                transformArray[1] = (float.Parse(transformArray[1]) * _sizeScale).ToString("f2");
                transformArray[2] = (float.Parse(transformArray[2]) * _sizeScale).ToString("f2");
                transformArray[3] = (float.Parse(transformArray[3]) * _sizeScale).ToString("f2");
                transformArray[4] = (float.Parse(transformArray[4]) * _sizeScale + (isOriginal ? _offset1 : 0) + _offset2).ToString("f2");
                transformArray[5] = (float.Parse(transformArray[5]) * _sizeScale).ToString("f2");
                var duplicateTransformStr = string.Join(" ", transformArray);
                gNode.Attributes["transform"].Value = $"matrix({duplicateTransformStr})";
            }

            var rectNode = resultNode.SelectSingleNode("//svg:rect", _manager);
            {
                var id = rectNode.Attributes["id"].Value;
            }

            return resultNode;
        }

        private XmlNode GenerateClipPathNode(XmlNode node, bool isOriginal)
        {
            return null;
        }

        private void UpdateImageNodeId(XmlNode node, int offset)
        {
            var pattern = @"\d+";
            var gIDAttribute = node.Attributes["id"];
            if (gIDAttribute != null)
            {
                var gID = gIDAttribute.Value;
                var gID_num = Convert.ToInt32(Regex.Match(gID, pattern).Groups[0].Value);
                gIDAttribute.Value = gID.Replace($"canvasFront_image_{gID_num}", $"canvasFront_image_{gID_num + offset}");
            }

            var clipPathID = "";
            var clipPathID_num = 0;
            if (node.SelectSingleNode("//*[contains(@id,'CLIPPATH')]", _manager) != null)
            {
                var clipPathIDAttribute = node.SelectSingleNode("//*[contains(@id,'CLIPPATH')]", _manager).Attributes["id"];
                if (clipPathIDAttribute != null)
                {
                    clipPathID = clipPathIDAttribute.Value;
                    clipPathID_num = Convert.ToInt32(Regex.Match(clipPathID, pattern).Groups[0].Value);
                    clipPathIDAttribute.Value = clipPathID.Replace($"CLIPPATH_{clipPathID_num}", $"CLIPPATH_{clipPathID_num + offset}");
                }
            }

            var imageIDAttribute = node.SelectSingleNode("//*[@xlink:href]", _manager).Attributes["id"];
            if (imageIDAttribute != null)
            {
                var imageID = imageIDAttribute.Value;
                var imageID_num = Convert.ToInt32(Regex.Match(imageID, pattern).Groups[0].Value);
                imageIDAttribute.Value = imageID.Replace($"i{imageID_num}", $"i{imageID_num + offset}");
            }

            var clipPathAttribute = node.Attributes["clip-path"];
            if (clipPathAttribute != null)
            {
                clipPathAttribute.Value = $"url(#{clipPathID.Replace($"CLIPPATH_{clipPathID_num}", $"CLIPPATH_{clipPathID_num + offset}")})";
            }
        }

        private void UpdateTextNodeId(XmlNode node, bool isOriginal, double sizeScale)
        {
            var pattern = @"\d+";
            var gIDAttribute = node.Attributes["id"];
            if (gIDAttribute != null)
            {
                var gID = node.Attributes["id"].Value;
                var gID_num = Convert.ToInt32(Regex.Match(gID, pattern).Groups[0].Value);
                gIDAttribute.Value = gID.Replace($"{gID_num}", $"{gID_num * 2 - (isOriginal ? 1 : 0)}");
            }

            if (node.SelectSingleNode("//svg:text", _manager) != null)
            {
                var textIDAttribute = node.SelectSingleNode("//svg:text", _manager).Attributes["id"];
                if (textIDAttribute != null)
                {
                    var textID = textIDAttribute.Value;
                    var textID_num = Convert.ToInt32(Regex.Match(textID, pattern).Groups[0].Value);
                    textIDAttribute.Value = textID.Replace($"{textID_num}", $"{textID_num * 2 - (isOriginal ? 1 : 0)}");

                    var textFormIDAttribute = node.SelectSingleNode("//svg:text", _manager).Attributes["cpbv:formId"];
                    if (textFormIDAttribute != null)
                    {
                        if (_textElementDict.ContainsKey(textIDAttribute.Value))
                        {
                            textFormIDAttribute.Value = _textElementDict[textIDAttribute.Value];
                        }
                    }
                }
            }

            // textNode need to add a offset for <tspan> node's x attribute, offset = 7/10 * font-size
            var tspanNodes = node.SelectNodes("//svg:tspan", _manager);
            if (tspanNodes != null && tspanNodes.Count > 0)
            {
                var textFontSize = Convert.ToInt32(node.SelectSingleNode("//*[@font-size]").Attributes["font-size"].Value);
                for (int i = 0; i < tspanNodes.Count; i++)
                {
                    var tspanNode = tspanNodes[i];
                    var tspanX = Convert.ToDouble(tspanNode.Attributes["x"].Value);
                    var tspanY = Convert.ToDouble(tspanNode.Attributes["y"].Value);

                    if (tspanNodes.Count == i + 1 && tspanNodes.Count > 1)
                    {
                        tspanNode.Attributes["x"].Value = (tspanX + (4.0 / 10.0 * textFontSize)).ToString();
                    }
                    else
                    {
                        tspanNode.Attributes["x"].Value = (tspanX + (7.0 / 10.0 * textFontSize)).ToString();
                    }
                    tspanNode.Attributes["y"].Value = (tspanY * sizeScale).ToString();
                }
            }
        }

        private void AddMaskLayer(XmlNode node)
        {
            var defs = _xmlDoc.CreateElement("defs", _xmlDoc.DocumentElement.NamespaceURI);
            if (defs.Attributes["xmlns"] != null)
            {
                defs.RemoveAttribute("xmlns");
            }

            defs.AppendChild(GenerateMaskChildNode("originalMask", _offset2 + _offset1, 0, _sizeScale * _originalWidth, _svgHeight));
            defs.AppendChild(GenerateMaskChildNode("duplicateMask", _offset2, 0, _sizeScale * _originalWidth, _svgHeight));
            node.AppendChild(defs);
        }

        private XmlNode GenerateMaskChildNode(string id, double x, double y, double width, double height)
        {
            var mask = _xmlDoc.CreateElement("mask", _xmlDoc.DocumentElement.NamespaceURI);
            mask.SetAttribute("id", id);
            mask.SetAttribute("x", 0.ToString());
            mask.SetAttribute("y", 0.ToString());

            var mask_rect = _xmlDoc.CreateElement("rect", _xmlDoc.DocumentElement.NamespaceURI);
            mask_rect.SetAttribute("x", x.ToString());
            mask_rect.SetAttribute("y", y.ToString());
            mask_rect.SetAttribute("width", width.ToString());
            mask_rect.SetAttribute("height", height.ToString());
            mask_rect.SetAttribute("fill", "white");
            mask.AppendChild(mask_rect);
            return mask;
        }

        private void AddContainerNode(XmlNode node)
        {
            _originalContainer = _xmlDoc.CreateElement("g", _xmlDoc.DocumentElement.NamespaceURI);
            _originalContainer.SetAttribute("mask", "url(#originalMask)");
            if (_originalContainer.Attributes["xmlns"] != null)
            {
                _originalContainer.RemoveAttribute("xmlns");
            }

            _duplicateContainer = _xmlDoc.CreateElement("g", _xmlDoc.DocumentElement.NamespaceURI);
            _duplicateContainer.SetAttribute("mask", "url(#duplicateMask)");
            if (_duplicateContainer.Attributes["xmlns"] != null)
            {
                _duplicateContainer.RemoveAttribute("xmlns");
            }

            node.AppendChild(_originalContainer);
            node.AppendChild(_duplicateContainer);
        }

        private XmlNamespaceManager GetNamespaceManager(XmlDocument xmlDoc)
        {
            XPathNavigator navigator = xmlDoc.CreateNavigator();
            XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
            navigator.MoveToRoot();
            if (navigator.HasChildren)
            {
                navigator.MoveToFirstChild();
                navigator.MoveToFirstNamespace();
                manager.AddNamespace(navigator.Name, navigator.Value);

                while (navigator.MoveToNextNamespace())
                {
                    manager.AddNamespace(navigator.Name == string.Empty ? "svg" : navigator.Name, navigator.Value);
                }
            }
            return manager;
        }
    }
}
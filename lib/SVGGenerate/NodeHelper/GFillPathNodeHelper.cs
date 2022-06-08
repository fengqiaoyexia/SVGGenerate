using System.Xml;

namespace SVGGenerate
{
    internal class GFillPathNodeHelper
    {
        public static void Handle(SVGGenerateModel model)
        {
            var gFillPathNode = model.Root.SelectSingleNode("//svg:g[@fill]", model.Manager);
            if (gFillPathNode != null)
            {
                UpdateGFillPath(gFillPathNode, model);
            }
        }

        private static void UpdateGFillPath(XmlNode gFillPathNode, SVGGenerateModel model)
        {
            var parentNode = gFillPathNode.ParentNode;

            var orginalNode = GenerateGFillPathNode(gFillPathNode, true, model);
            var duplicateNode = GenerateGFillPathNode(gFillPathNode, false, model);

            parentNode.RemoveChild(gFillPathNode);
            model.OriginalContainer.AppendChild(orginalNode);
            model.DuplicateContainer.AppendChild(duplicateNode);
        }

        private static XmlNode GenerateGFillPathNode(XmlNode node, bool isOriginal, SVGGenerateModel model)
        {
            var resultNode = node.CloneNode(true);

            var transform = resultNode.Attributes["transform"].Value;
            var n1 = transform.IndexOf('(');
            var n2 = transform.IndexOf(')');
            var transformStr = transform.Substring(n1 + 1, n2 - n1 - 1);
            var transformArray = transformStr.Contains(',') ? transformStr.Split(",") : transformStr.Split(" ");
            transformArray[0] = (float.Parse(transformArray[0]) * model.SizeScale).ToString("f2");
            transformArray[1] = (float.Parse(transformArray[1]) * model.SizeScale).ToString("f2");
            transformArray[2] = (float.Parse(transformArray[2]) * model.SizeScale).ToString("f2");
            transformArray[3] = (float.Parse(transformArray[3]) * model.SizeScale).ToString("f2");
            transformArray[4] = (float.Parse(transformArray[4]) * model.SizeScale + (isOriginal ? model.Offset1 : 0) + model.Offset2).ToString("f2");
            transformArray[5] = (float.Parse(transformArray[5]) * model.SizeScale).ToString("f2");
            var duplicateTransformStr = string.Join(" ", transformArray);
            resultNode.Attributes["transform"].Value = $"matrix({duplicateTransformStr})";

            return resultNode;
        }
    }
}
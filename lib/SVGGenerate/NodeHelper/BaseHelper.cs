using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace SVGGenerate
{
    internal static class BaseHelper
    {
        public static SVGGenerateModel GenerateSVGGenerateModel(Stream stream, int svgWidth, int svgHeight)
        {
            var model = new SVGGenerateModel
            {
                XmlDoc = new XmlDocument()
            };
            model.XmlDoc.Load(stream);
            model.Root = model.XmlDoc.DocumentElement;
            model.Manager = GetNamespaceManager(model.XmlDoc);
            model.SvgWidth = svgWidth;
            model.SvgHeight = svgHeight;
            model.OriginalHeight = Convert.ToInt32(Convert.ToDouble(model.Root.Attributes["height"].Value));
            model.OriginalWidth = Convert.ToInt32(Convert.ToDouble(model.Root.Attributes["width"].Value));
            var heightScale = model.SvgHeight * 1.0 / model.OriginalHeight * 1.0;
            var widthScale = (model.SvgWidth / 2) * 1.0 / model.OriginalWidth * 1.0;
            model.SizeScale = heightScale < widthScale ? heightScale : widthScale;
            model.Offset1 = model.SvgWidth / 2;
            model.Offset2 = (model.SvgWidth / 2 - model.SizeScale * model.OriginalWidth) / 2;
            return model;
        }

        private static XmlNamespaceManager GetNamespaceManager(XmlDocument xmlDoc)
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
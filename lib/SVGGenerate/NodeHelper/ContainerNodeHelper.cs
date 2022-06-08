using System.Xml;

namespace SVGGenerate
{
    internal static class ContainerNodeHelper
    {
        public static void Handle(SVGGenerateModel model)
        {
            model.OriginalContainer = GenerateContainerNode(model, "originalMask");
            model.DuplicateContainer = GenerateContainerNode(model, "duplicateMask");
        }

        private static XmlElement GenerateContainerNode(SVGGenerateModel model, string containerMaskUrl)
        {
            var originalContainer = model.XmlDoc.CreateElement("g", model.XmlDoc.DocumentElement.NamespaceURI);
            originalContainer.SetAttribute("mask", $"url(#{containerMaskUrl})");
            if (originalContainer.Attributes["xmlns"] != null)
            {
                originalContainer.RemoveAttribute("xmlns");
            }

            return originalContainer;
        }
    }
}
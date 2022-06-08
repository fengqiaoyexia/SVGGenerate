using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;

namespace SVGGenerate
{
    public class SvgGenerate
    {
        private SVGGenerateModel _model = new SVGGenerateModel();

        public SvgGenerate(Stream stream, int svgWidth, int svgHeight)
        {
            if (stream.Position > 0)
            {
                stream.Position = 0;
            }
            _model = BaseHelper.GenerateSVGGenerateModel(stream, svgWidth, svgHeight);
        }

        public MemoryStream GenerateDuplicateIamgeStream()
        {
            var outStream = new MemoryStream();

            ParentSVGNodeHelper.Handle(_model);

            ContainerNodeHelper.Handle(_model);

            ParameterNodeHelper.Handle(_model);

            GFillPathNodeHelper.Handle(_model);

            //var defsNodes = root.SelectNodes("//svg:defs", _manager);
            //if (defsNodes?.Count > 0)
            //{
            //    UpdateDefsNode(defsNodes.Cast<XmlNode>().ToList());
            //}

            //var clipPathNodes = root.SelectNodes("//svg:clipPath", _manager);
            //if (clipPathNodes?.Count > 0)
            //{
            //    UpdateClipPathNode(clipPathNodes.Cast<XmlNode>().ToList());
            //}

            //var gImageNodes = root.SelectNodes("//*[@cpbv:class='image']", _manager);
            //if (gImageNodes?.Count > 0)
            //{
            //    UpdateGImageNodes(gImageNodes.Cast<XmlNode>().ToList());
            //}

            //var gTextNodes = root.SelectNodes("//*[@cpbv:class='text']", _manager);

            //if (gTextNodes?.Count > 0)
            //{
            //    UpdateGTextNodes(gTextNodes.Cast<XmlNode>().ToList());
            //}

            //AddMaskLayer(root);

            _model.XmlDoc.Save(outStream);
            outStream.Position = 0;
            return outStream;
        }


    }

    internal class SVGGenerateModel
    {
        public XmlDocument XmlDoc;
        public XmlNamespaceManager Manager;
        public XmlElement Root;
        public int SvgWidth = 1000;
        public int SvgHeight = 361;
        public int OriginalWidth;
        public int OriginalHeight;
        public double SizeScale;
        public double Offset1;
        public double Offset2;
        public Dictionary<string, string> TextElementDict = new Dictionary<string, string>();
        public XmlElement OriginalContainer;
        public XmlElement DuplicateContainer;
    }
}
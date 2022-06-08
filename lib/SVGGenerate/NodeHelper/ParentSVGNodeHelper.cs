namespace SVGGenerate
{
    internal static class ParentSVGNodeHelper
    {
        public static void Handle(SVGGenerateModel model)
        {
            model.Root.Attributes["width"].Value = model.SvgWidth.ToString();
            model.Root.Attributes["height"].Value = model.SvgHeight.ToString();
            if (model.Root.Attributes["viewBox"] != null)
            {
                model.Root.Attributes["viewBox"].Value = $"0 0 {model.SvgWidth} {model.SvgHeight}";
            }
        }
    }
}
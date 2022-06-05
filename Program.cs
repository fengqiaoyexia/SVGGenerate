using System;
using System.IO;

namespace SingleToDuplciate
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            SvgTest();
        }

        private static void SvgTest()
        {
            var name = Console.ReadLine();
            while (name != "end")
            {
                var svgStream = new MemoryStream(File.ReadAllBytes(@$"..\..\..\Images\{name}.svg"));
                var svg = new SvgGenerate(svgStream, 1000, 361);
                var tt = svg.GenerateDuplicateIamgeStream();
                File.WriteAllBytes(@$"..\..\..\Images\{name}-D.svg", tt.ToArray());
                Console.WriteLine("Sucessed for convert");
                name = Console.ReadLine();
            }
            Console.ReadKey();
        }

        private static void PngTest()
        {
            var name = "131134870";
            var pngStream = new MemoryStream(File.ReadAllBytes(@$"..\Images\{name}.png"));
            var png = new PNGGenerate();
            var tt = png.GenerateDuplicateIamgeStream(pngStream);
            File.WriteAllBytes(@$"..\Images\{name}-D.png", ((MemoryStream)tt).ToArray());
        }
    }
}
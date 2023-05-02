using System.Drawing;

namespace PhotoGenerator.Models.Classes
{
    public class FileImage
    {
        public string Path { get; }
        public Color MeanColor { get; }
        public (float, float, float) LABColor { get; }

        public FileImage(string path, Color meanColor, (float, float, float) lABColor)
        {
            this.Path = path;
            this.MeanColor = meanColor;
            this.LABColor = lABColor;
        }
    }
}

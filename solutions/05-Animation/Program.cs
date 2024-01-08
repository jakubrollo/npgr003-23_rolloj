using CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;

namespace _05_Animation
{
    public enum MandalaNumberType
    {
        Pi,
        GoldenRatio,
        Euler,
        Sqrt2,
        CustomDouble
    }

    public class Options
    {
        private int speed = 50;

        [Option('w', "width", Required = true, HelpText = "Image width in pixels.")]
        public int Width { get; set; }

        [Option('h', "height", Required = true, HelpText = "Image height in pixels.")]
        public int Height { get; set; }

        [Option('f', "frames", Required = false, Default = 500, HelpText = "Number of output frames.")]
        public int Frames { get; set; } = 500;

        [Option('s', "speed", Required = false, Default = 50, HelpText = "Speed of drawing.")]
        public int Speed
        {
            get => speed;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Speed), "Speed must be greater than 1.");
                }
                speed = value;
            }
        }

        [Option('o', "output", Required = false, Default = "anim/out{0:0000}.png", HelpText = "Output file-name mask.")]
        public string FileMask { get; set; } = "anim/out{0:0000}.png";

        [Option('n', "number", Required = false, Default = "Pi", HelpText = "Type of the mandala number.")]
        public string Number { get; set; } = "Pi";
        public MandalaNumberType MandalaNumberType { get; set; }

        public double CustomNumberValue { get; set; }

        public void ParseNumber()
        {
            if (double.TryParse(Number, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var customNumber))
            {
                MandalaNumberType = MandalaNumberType.CustomDouble;
                CustomNumberValue = customNumber;
            }
            else if (Enum.TryParse<MandalaNumberType>(Number, true, out var type))
            {
                MandalaNumberType = type;
            }
            else
            {
                throw new ArgumentException($"Invalid value for 'number': {Number}");
            }
        }

        [Option('j', "jump", Required = false, Default = 0, HelpText = "Skips the generation of images for the given amount of frames.")]
        public int FramesToSkip { get; set; } = 0;

    }

    class MandalaDrawer
    {
        private double speed;
        private double lastPos = 301.16204775212566;
        private double numberToDraw;
        public MandalaDrawer(int speed, MandalaNumberType numberType, double givenNumber)
        {
            switch (numberType)
            {
                case MandalaNumberType.Pi:
                    numberToDraw = Math.PI;
                    break;
                case MandalaNumberType.GoldenRatio:
                    numberToDraw = (1 + Math.Sqrt(5)) / 2;
                    break;
                case MandalaNumberType.Sqrt2:
                    numberToDraw = Math.Sqrt(2);
                    break;
                case MandalaNumberType.Euler:
                    numberToDraw = Math.Exp(1);
                    break;
                case MandalaNumberType.CustomDouble:
                    numberToDraw = givenNumber;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(numberType), "Invalid MandalaNumberType.");
            }

            this.speed = (double)speed*0.0004;
        }

        public void DrawFrameOfMandala(Image<Rgba32> image)
        {
            double[] theta = new double[700];
            Complex[] z = new Complex[700];
            int repetitions = 0;

            for (int i = 0; i < theta.Length; i++)
            {
                theta[i] = ((i * speed) * 2 * Math.PI / theta.Length) + lastPos;
                z[i] = CalculateZ(theta[i]);
                repetitions = i;
            }

            lastPos = theta[repetitions];
            PointF[] points = new PointF[repetitions + 1];
            for (int i = 0; i < repetitions + 1; i++)
            {
                points[i] = new PointF((float)(image.Width / 2 + z[i].Real * image.Width / 4),
                                       (float)(image.Height / 2 + z[i].Imaginary * image.Height / 4));
            }
            Pen pen = Pens.Solid(Color.WhiteSmoke, 1);
            image.Mutate(x => x.DrawLine(pen, points));
        }

        private Complex CalculateZ(double theta)
        {
            Complex innerArm = Complex.Exp(Complex.ImaginaryOne * theta);
            Complex outerArm = Complex.Exp(numberToDraw * Complex.ImaginaryOne * theta);
            return innerArm + outerArm;
        }
    }

    internal class Program
    {
        private static Rgba32 BackgroundColor = new(0xFF, 0xFF, 0xFF);
        private static Rgba32 DrawColor = new(0x10, 0x20, 0x80);

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                o.ParseNumber();
                if (o.Frames <= 0)
                {
                    Console.WriteLine($"Error: The number of frames must be set to a positive number.");
                }
                string directoryPath = "anim";
                System.IO.Directory.CreateDirectory(directoryPath);

                var image = new Image<Rgba32>(o.Width, o.Height);

                MandalaDrawer drawer = new(o.Speed, o.MandalaNumberType, o.CustomNumberValue);

                for(int skipFrame = 0; skipFrame < o.FramesToSkip; skipFrame++)
                {
                    drawer.DrawFrameOfMandala(image);
                }

                for (int frame = 0; frame < o.Frames; frame++)
                {
                    if (frame % 30 == 0 && frame != 0)
                    {

                        image = Image.Load<Rgba32>(string.Format(o.FileMask, frame - 1));
                    }
                        
                    drawer.DrawFrameOfMandala(image);

                    string fileName = string.Format(o.FileMask, frame);
                    image.Save(fileName);

                    Console.WriteLine($"Frame '{fileName}' created successfully.");
                }
                   
            });
        }
    }
}

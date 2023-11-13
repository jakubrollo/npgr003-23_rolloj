using CommandLine;
using SixLabors.ImageSharp.ColorSpaces.Conversion;

namespace _03_SFC;

public enum ColorChannel
{
  R,
  G,
  B,
  Gray
}

public class Options
{
  [Option('i', "input", Required = true, Default = "", HelpText = "Input file-name.")]
  public string FileName { get; set; } = string.Empty;

  [Option('m', "method", Required = false, Default = 0, HelpText = "Pixel order method (-1 for the best one).")]
  public int Method { get; set; } = 0;

  [Option('p', "predictor", Required = false, Default = 1, HelpText = "Predictor order.")]
  public int Predictor { get; set; } = 1;

  [Option('c', "channel", Required = false, Default = ColorChannel.Gray, HelpText = "Color channel to analyze (R, G, B, or Gray).")]
  public ColorChannel Channel { get; set; } = ColorChannel.Gray;

  [Option('d', "draw", Required = false, Default = false, HelpText = "Optional drawing visualisation in console of the selected method.")]
  public bool drawActivated { get; set; } = false;
}

internal class Program
{
  static void Main(string[] args)
  {

    Parser.Default.ParseArguments<Options>(args)
      .WithParsed<Options>(o =>
      {
        if (string.IsNullOrEmpty(o.FileName) ||
            !File.Exists(o.FileName))
        {
          Console.WriteLine($"Invalid image '{o.FileName}'.");
          return;
        }

        Draw drawer = new();
        Image<Rgba32> image;
        try
        {
          var im = Image.Load(o.FileName);
          image = im.CloneAs<Rgba32>();
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
          return;
        }
        int width  = image.Width;
        int height = image.Height;
        bool dirty = false;   // don't report any more error messages, ignore the rest of the image...

        // SFC checks.
        HashSet<int> processed = new();

        // Duplicity check function.
        bool checkPixel(int x, int y)
        {
          int index = y * width + x;
          if (processed.Contains(index))
          {
            Console.WriteLine($"Duplicate image access [{x},{y}].");
            return dirty = true;
          }
          processed.Add(index);
          return false;
        }

        // We can proceed (image, width, height are valid).

        // Image processing: Predictor, PredictiveEncoder, EntropyCalculator.
        EntropyCalculator entropyCalculator = new();
        IPredictor predictor;
        string predictorName;
        switch (o.Predictor)
        {
          case 1:
            predictor = new LinearPredictor1();
            predictorName = "Linear Predictor";
            break;
          case 2:
            predictor = new LinearPredictor2();
            predictorName = "Second-order Predictor";
            break;
          default:
            predictor = new LinearPredictor1();
            predictorName = "Linear Predictor";
            break;
        }
        PredictiveEncoder encoder = new(predictor, entropyCalculator);

        // What to do with the individual pixel?
        void pixelAction (int x, int y)
        {
          if (dirty)
            return;

          if (x < 0 || x >= width ||
              y < 0 || y >= height)
          {
            Console.WriteLine($"Invalid image access [{x},{y}].");
            dirty = true;
            return;
          }

          // Duplicity check.
          if (checkPixel(x, y))
            return;
          if (o.drawActivated)
          {
            drawer.AddCoordinate(x, y);
          }
          var pixel = image[x, y];

          int colorValue;
          switch (o.Channel)
          {
            case ColorChannel.R:
              colorValue = pixel.R;
              break;
            case ColorChannel.G:
              colorValue = pixel.G;
              break;
            case ColorChannel.B:
              colorValue = pixel.B;
              break;
            case ColorChannel.Gray:
            default:
              // Default behavior is to compute gray value (Y).
              var YCRCB = ColorSpaceConverter.ToYCbCr(pixel);
              colorValue = (int)Math.Round(YCRCB.Y);
              break;
          }

          // Pass the value to the encoder.
          encoder.Put(colorValue);
        }

        string order;
        // Pixel order method.
        IPixelOrder sfc;
        switch (o.Method)
        {
          case 0:
            sfc = new ScanLine(pixelAction);
            order = "ScanLine";
            break;
          case 1:
            sfc = new ZigZagScan(pixelAction);
            order = "ZigZag scan";
            break;
          case 2:
            sfc = new SpiralPixelOrder(pixelAction);
            order = "Rectangle Spiral";
            break;
          case 3:
            sfc = new ZCurve(pixelAction);
            order = "Z-order Curve";
            break;
          case 4:
            sfc = new PeanoCurve(pixelAction);
            order = "Peano Curve";
            break;
          case 5:
            sfc = new HilbertCurve(pixelAction);
            order = "Hilbert Curve";
            break;
          case -1:
            sfc = new HilbertCurve(pixelAction);
            order = "Hilbert Curve";
            break;

          default:
            Console.WriteLine($"Invalid method {o.Method}.");
            return;
        }

        // Pass the image.
        sfc.Pass(width, height);
        if (o.drawActivated)
        {
          drawer.DrawAll();
        }

        if (dirty) return;

        if (processed.Count != width * height)
        {
          Console.WriteLine($"Not all pixels were passed ({processed.Count} < {width} x {height}).");
          for (int i = 0; i < width * height; i++)
            if (!processed.Contains(i))
            {
              Console.WriteLine($"Pixel [{i % width},{i / width}] was not processed.");
              break;
            }
          return;
        }

        // Compute and print the entropy.
        long entropy = entropyCalculator.Entropy();
        Console.WriteLine($"{entropy}");
        Console.WriteLine($"Image: {o.FileName}[{width}x{height}]");
        Console.WriteLine($"Order: {order}");
        Console.WriteLine(FormattableString.Invariant($"Average entropy: {entropy/(double)(width * height):f2} bits per pixel"));
        Console.WriteLine($"Predictor used: {predictorName}");
      });
  }
}

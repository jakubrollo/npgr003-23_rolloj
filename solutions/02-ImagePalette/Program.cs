using System;
using System.IO;
using System.Collections.Generic;
using CommandLine;
using System.Xml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Accord.MachineLearning;
using Accord.Statistics.Kernels;
using Accord.Statistics.Models.Regression;
using System.Drawing;
using System.Linq;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using Accord.Math.Distances;

public class Options
{
  [Option('i', "input", Required = true, HelpText = "Input image file.")]
  public string InputFile { get; set; }

  [Option('c', "colors", Required = true, HelpText = "Number of desired colors 3-10.")]
  public int NumColors { get; set; }

  [Option('o', "output", Required = false, HelpText = "Output file for the palette.")]
  public string OutputFile { get; set; }

  [Option('s', "slower", Required = false, Default = false, HelpText = "Slower but more precise calculation of the colour palette.")]
  public bool slower { get; set; }

  [Option('v', "visualize", Required = false, Default = null, HelpText = "Visualize palette.")]
  public string VisualizationOutputFile { get; set; }
}


public class PaletteGenerator
{
  public static void RunImagePaletteGenerator (Options options)
  {
    if (options.NumColors < 3 || options.NumColors > 10)
    {
      Console.WriteLine("Error: Number of colors must be in the range of 3 to 10.");
      return;
    }

    if (!File.Exists(options.InputFile))
    {
      Console.WriteLine("Error: Input file doesn't exist.");
      return;
    }

    var image = Image.Load<Rgba32>(options.InputFile);

    if (!options.slower)
    {
      // Check if the image is larger than 256x256 and resize if necessary for faster processing
      if (image.Width > 256 && image.Height > 256)
      {
        image = ResizeImageWithMinSide(image, 256);
      }
    }

    // Extract colors from the image and compute the palette
    List<Rgba32> palette = GeneratePalette(image, options.NumColors, options.VisualizationOutputFile);

    // Output the palette in the desired format (text, PNG, or SVG)
    if (options.OutputFile != null)
    {
      SavePaletteAsImage(palette, options.OutputFile);
    }
    else
    {
      PrintPalette(palette);
    }
  }

  public static Image<Rgba32> ResizeImageWithMinSide (Image<Rgba32> image, int minSide)
  {
    int newWidth, newHeight;

    if (image.Width < image.Height)
    {
      newWidth = minSide;
      newHeight = (int)Math.Ceiling((double)image.Height / image.Width * minSide);
    }
    else
    {
      newWidth = (int)Math.Ceiling((double)image.Width / image.Height * minSide);
      newHeight = minSide;
    }

    return image.Clone(x => x.Resize(new SixLabors.ImageSharp.Size(newWidth, newHeight)));
  }

  public static List<Rgba32> GeneratePalette (Image<Rgba32> image, int maxColors, string visualizationOutputFile)
  {
    ColorSpaceConverter converter = new ColorSpaceConverter();

    // Convert the image to an array of colors in CIE Lab space
    List<CieLab> pixels = new List<CieLab>();

    for (int y = 0; y < image.Height; y++)
    {
      for (int x = 0; x < image.Width; x++)
      {
        Rgba32 pixelColor = image[x, y];
        CieLab labColor = converter.ToCieLab(pixelColor);
        pixels.Add(labColor);
      }
    }

    if (maxColors > pixels.Count)
    {
      maxColors = pixels.Count;
    }

    // Define the weights for each dimension in CIE Lab space
    double[] weights = new double[] { 0.155, 1, 0.6 }; // Adjust the weights as needed

    // Use K-Means clustering with custom weights
    KMeans kmeans = new KMeans(k: maxColors)
    {
      Distance = new WeightedSquareEuclidean(weights)
    };

    KMeansClusterCollection clusters = kmeans.Learn(pixels.Select(p => new double[] { p.L, p.A, p.B }).ToArray());

    int[] clusterAssignments = clusters.Decide(pixels.Select(p => new double[] { p.L, p.A, p.B }).ToArray());
    double[][] clusterCenters = kmeans.Clusters.Centroids;

    // Determine the number of unique clusters
    int uniqueClusterCount = clusterAssignments.Distinct().Count();

    // Calculate the number of colors to output based on unique clusters
    int numColorsToOutput = Math.Clamp(uniqueClusterCount, 1, maxColors);

    if (visualizationOutputFile != null)
    {
      // Visualize and save the clustered image
      Image<Rgba32> paletteImage = VisualizePalette(image, numColorsToOutput, clusterAssignments, clusterCenters, converter);
      paletteImage.Save(visualizationOutputFile);
    }

    // Retrieve the cluster centers as colors in RGB
    List<Rgba32> palette = new List<Rgba32>();
    for (int i = 0; i < numColorsToOutput; i++)
    {
      double[] clusterCenter = clusterCenters[i];
      CieLab labColor = new CieLab((float)clusterCenter[0], (float)clusterCenter[1], (float)clusterCenter[2]);
      palette.Add(converter.ToRgb(labColor));
    }

    palette = SortPaletteByBrightness(palette);

    return palette;
  }

  public static List<Rgba32> SortPaletteByBrightness (List<Rgba32> palette)
  {
    // Sort the palette by brightness (luminance)
    List<Rgba32> sortedPalette = palette.OrderBy(color =>
    {
      // Calculate luminance for each color
      double luminance = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
      return luminance;
    }).ToList();

    return sortedPalette;
  }

  public static Image<Rgba32> VisualizePalette (Image<Rgba32> image, int numColors, int[] clusterAssignments, double[][] clusterCenters, ColorSpaceConverter converter)
  {
    int width = image.Width;
    int height = image.Height;

    // Create a new image for visualization
    Image<Rgba32> clusteredImage = new Image<Rgba32>(width, height);

    // Assign cluster colors to pixels in the new image
    for (int y = 0; y < height; y++)
    {
      for (int x = 0; x < width; x++)
      {
        int pixelIndex = y * width + x;
        int clusterIndex = clusterAssignments[pixelIndex];
        CieLab cieLabClusterColor = new CieLab((float)clusterCenters[clusterIndex][0], (float)clusterCenters[clusterIndex][1], (float)clusterCenters[clusterIndex][2]);
        clusteredImage[x, y] = converter.ToRgb(cieLabClusterColor);
      }
    }

    return clusteredImage;
  }

  public static void PrintPalette (List<Rgba32> palette)
  {
    foreach (Rgba32 color in palette)
    {
      Console.WriteLine($"{color.R}, {color.G}, {color.B}");
    }
  }

  public static void SavePaletteAsImage (List<Rgba32> palette, string outputFile)
  {
    if (outputFile.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
    {
      // Save as SVG format
      SavePaletteAsSvg(palette, outputFile);
    }
    else if (outputFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
    {
      // Save as PNG format
      SavePaletteAsPng(palette, outputFile);
    }
    else
    {
      // Handle unsupported file formats or provide an error message
      Console.WriteLine("Unsupported file format. Please use .svg or .png.");
    }
  }

  public static void SavePaletteAsSvg (List<Rgba32> palette, string outputFile)
  {
    int RectWidth = 200;
    int imageWidth = palette.Count * RectWidth;
    int imageHeight = RectWidth;

    // Create an XML document to represent the SVG
    XmlDocument svgDoc = new XmlDocument();

    // Create the SVG root element
    XmlElement svgRoot = svgDoc.CreateElement("svg");
    svgRoot.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
    svgRoot.SetAttribute("width", imageWidth.ToString());
    svgRoot.SetAttribute("height", imageHeight.ToString());
    svgDoc.AppendChild(svgRoot);

    // Create a group element to contain the color rectangles
    XmlElement group = svgDoc.CreateElement("g");
    svgRoot.AppendChild(group);

    for (int i = 0; i < palette.Count; i++)
    {
      // Create a rectangle element for each color
      XmlElement rect = svgDoc.CreateElement("rect");
      rect.SetAttribute("x", (i * RectWidth).ToString());
      rect.SetAttribute("y", "0");
      rect.SetAttribute("width", RectWidth.ToString());
      rect.SetAttribute("height", imageHeight.ToString());
      rect.SetAttribute("fill", $"#{palette[i].R:X2}{palette[i].G:X2}{palette[i].B:X2}");
      group.AppendChild(rect);
    }

    // Save the SVG document to a file
    svgDoc.Save(outputFile);
  }

  public static void SavePaletteAsPng (List<Rgba32> palette, string outputFile)
  {
    int RectWidth = 200;

    int imageWidth = palette.Count * RectWidth;
    int imageHeight = RectWidth;

    using (var outputImage = new Image<Rgba32>(imageWidth, imageHeight))
    {
      for (int i = 0; i < palette.Count; i++)
      {
        for (int x = i * RectWidth; x < (i + 1) * RectWidth; x++)
        {
          for (int y = 0; y < imageHeight; y++)
          {
            outputImage[x, y] = palette[i];
          }
        }
      }

      outputImage.Save(outputFile);
    }
  }
}

class Program
{
  static void Main (string[] args)
  {
    Parser.Default.ParseArguments<Options>(args)
        .WithParsed(options => PaletteGenerator.RunImagePaletteGenerator(options));
  }
}

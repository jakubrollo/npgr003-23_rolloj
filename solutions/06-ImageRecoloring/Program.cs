using System;
using CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;

class Program
{
  class Options
  {
    [Option('i', "input", Required = true, HelpText = "Input image path")]
    public string InputPath { get; set; }

    [Option('o', "output", Required = true, HelpText = "Output image path")]
    public string OutputPath { get; set; }

    [Option('h', "hue-delta", Required = true, HelpText = "Hue delta for recoloring")]
    public float HueDelta { get; set; }
  }

  static void Main (string[] args)
  {
    Parser.Default.ParseArguments<Options>(args)
        .WithParsed(options => RecolorImage(options.InputPath, options.OutputPath, options.HueDelta));
  }

  static void RecolorImage (string inputPath, string outputPath, float hueDelta)
  {
    using (var image = Image.Load<Rgba32>(inputPath))
    {
      ColorSpaceConverter converter = new ColorSpaceConverter();
      var skinThreshold = new SkinThreshold();
      // Matrix to store skin pixels
      double[,] skinMatrix = new double[image.Width, image.Height];

      for (int y = 0; y < image.Height; y++)
      {
        for (int x = 0; x < image.Width; x++)
        {
          Rgba32 pixel = image[x, y];
          var hsv = ColorSpaceConverter.ToHsv(pixel);
          var ycbcr = ColorSpaceConverter.ToYCbCr(pixel);
          CieLab labColor = converter.ToCieLab(pixel);

          if (skinThreshold.IsSkinPixel(hsv, ycbcr))
          {
            skinMatrix[x, y] = 0;
          }
          else if (skinThreshold.IsAlmostSkinPixel(labColor))
          {
            skinMatrix[x, y] = 0.2;
          }
          else
          {
            skinMatrix[x, y] = 1;
          }
        }
      }

      // Apply morphological erosion
      // int[,] erodedMatrix = Erosion(skinMatrix, image.Width, image.Height);

      for (int y = 0; y < image.Height; y++)
      {
        for (int x = 0; x < image.Width; x++)
        {
          Rgba32 pixel = image[x, y];
          var hsv = ColorSpaceConverter.ToHsv(pixel);

          // Calculate the likelihood ratio for skin probability based on the updated skin matrix
          var likelihoodRatio = skinMatrix[x, y];

          // Calculate new hueDelta based on likelihoodRatio
          float newHueDelta = (float)((double)hueDelta * likelihoodRatio);

          // Apply hue shift to non-skin pixels
          var newHsv = new Hsv(hsv.H + newHueDelta, hsv.S, hsv.V);
          var newRgb = ColorSpaceConverter.ToRgb(newHsv);
          image[x, y] = new Rgba32(newRgb.R, newRgb.G, newRgb.B, pixel.A);
          // image[x, y] = new Rgba32((byte)(255 * likelihoodRatio), (byte)(255 * likelihoodRatio), (byte)(255 * likelihoodRatio), pixel.A);
        }
      }

      image.Save(outputPath);
    }
  }

  static int[,] Erosion (double[,] inputMatrix, int width, int height)
  {
    int[,] resultMatrix = new int[width, height];

    for (int y = 1; y < height - 1; y++)
    {
      for (int x = 1; x < width - 1; x++)
      {
        // Apply 3x3 erosion kernel
        if (inputMatrix[x, y] == 0 ||
            inputMatrix[x - 1, y - 1] == 0 || inputMatrix[x, y - 1] == 0 || inputMatrix[x + 1, y - 1] == 0 ||
            inputMatrix[x - 1, y] == 0 || inputMatrix[x, y] == 0 || inputMatrix[x + 1, y] == 0 ||
            inputMatrix[x - 1, y + 1] == 0 || inputMatrix[x, y + 1] == 0 || inputMatrix[x + 1, y + 1] == 0)
        {
          if (inputMatrix[x, y] == 2)
          {
            resultMatrix[x, y] = 0;
          }
        }
        else
        {
          resultMatrix[x, y] = 1;
        }
      }
    }

    return resultMatrix;
  }
}

class SkinThreshold
{
  public bool IsSkinPixel (Hsv hsv, YCbCr ycbcr)
  {
    // Skin tone thresholds in HSV and YCbCr color spaces
    bool hsvCheck = (hsv.H >= 0 && hsv.H <= 35) && (hsv.S >= 0.05882 && hsv.S <= 0.666667) && (hsv.V >= 0 && hsv.V <= 1);
    bool ycbcrCheck = (ycbcr.Y >= 0 && ycbcr.Y <= 255) && (ycbcr.Cr >= 135 && ycbcr.Cr <= 180) && (ycbcr.Cb >= 85 && ycbcr.Cb <= 135);
    return hsvCheck || ycbcrCheck;
  }

  public bool IsAlmostSkinPixel (CieLab labColor)
  {
    // Skin tone thresholds in HSV and YCbCr color spaces
    bool labCheck = (labColor.L >= 30 && labColor.L <= 100) && (labColor.A >= -10 && labColor.A <= 45) && (labColor.B >= -10 && labColor.B <= 45);

    return labCheck;
  }

  public void MedianBlur (int[,] matrix, int width, int height)
  {
    // Copy the original matrix
    int[,] originalMatrix = new int[width, height];
    Array.Copy(matrix, originalMatrix, matrix.Length);

    for (int y = 1; y < height - 1; y++)
    {
      for (int x = 1; x < width - 1; x++)
      {
        // Extract the 3x3 neighborhood
        var neighborhood = new List<int>();
        for (int dy = -1; dy <= 1; dy++)
        {
          for (int dx = -1; dx <= 1; dx++)
          {
            neighborhood.Add(originalMatrix[x + dx, y + dy]);
          }
        }

        // Sort and set the median value
        neighborhood.Sort();
        matrix[x, y] = neighborhood[4];
      }
    }
  }
}

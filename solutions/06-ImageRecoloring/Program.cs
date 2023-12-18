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
          else
          {
            skinMatrix[x, y] = 1;
          }
        }
      }
      for (double i = 0; i < 10; i++)
      {
        skinMatrix = Dilation(skinMatrix, image.Width, image.Height, 1, 0);
      }
      // Apply morphological erosion
      
      for (double i = 0; i < 25; i ++)
      {
        skinMatrix = Erosion(skinMatrix, image.Width, image.Height, 0, 0);
      }

      for (int y = 0; y < image.Height; y++)
      {
        for (int x = 0; x < image.Width; x++)
        {
          Rgba32 pixel = image[x, y];
          CieLab labColor = converter.ToCieLab(pixel);

          if (skinThreshold.IsAlmostSkinPixel(labColor))
          {
            skinMatrix[x, y] = 0.1;
          }
        }
      }

      for (double i = 1; i-0.03125 >0; i -= 0.03125)
      {
        skinMatrix = Dilation(skinMatrix, image.Width, image.Height, i, 0.03125);
      }

      /*
      for (int y = 0; y < image.Height; y++)
      {
        for (int x = 0; x < image.Width; x++)
        {
          Rgba32 pixel = image[x, y];
          var hsv = ColorSpaceConverter.ToHsv(pixel);
          var ycbcr = ColorSpaceConverter.ToYCbCr(pixel);

          if (skinThreshold.IsSkinPixel2(hsv, ycbcr))
          {
            skinMatrix[x, y] = 0;
          }
        }
      }
      */

      for (int y = 0; y < image.Height; y++)
      {
        for (int x = 0; x < image.Width; x++)
        {
          Rgba32 pixel = image[x, y];
          var hsv = ColorSpaceConverter.ToHsv(pixel);

          // Calculate the likelihood ratio for skin probability based on the updated skin matrix
          var likelihoodRatio = skinMatrix[x, y];

          // Smooth transition using linear interpolation (LERP)
          var interpolatedRatio = SmoothTransition(likelihoodRatio);

          // Calculate new hueDelta based on the interpolated ratio
          float newHueDelta = (float)((double)hueDelta * interpolatedRatio);

          // Apply hue shift to non-skin pixels
          var newHsv = new Hsv((hsv.H + newHueDelta)%360, hsv.S, hsv.V);
          var newRgb = ColorSpaceConverter.ToRgb(newHsv);

          // Blend the new color with the original color based on the interpolated ratio

          //image[x, y] = new Rgba32((byte)(255 * likelihoodRatio), (byte)(255 * likelihoodRatio), (byte)(255 * likelihoodRatio), pixel.A);

          var blendedColor = BlendColors(pixel, new Rgba32(newRgb.R, newRgb.G, newRgb.B, pixel.A), interpolatedRatio);
          image[x, y] = blendedColor;

        }
      }

      image.Save(outputPath);
    }
  }

  static float SmoothTransition (double value)
  {
    // Use a smoothstep function for smooth transitions
    return (float)(value * value * (3 - 2 * value));
  }

  static Rgba32 BlendColors (Rgba32 color1, Rgba32 color2, float ratio)
  {
    // Linear interpolation (LERP) between two colors
    return new Rgba32(
        (byte)(color1.R + (color2.R - color1.R) * ratio),
        (byte)(color1.G + (color2.G - color1.G) * ratio),
        (byte)(color1.B + (color2.B - color1.B) * ratio),
        (byte)(color1.A + (color2.A - color1.A) * ratio)
    );
  }


  static double[,] Erosion (double[,] inputMatrix, int width, int height, double setValue, double newValue)
  {
    double[,] resultMatrix = new double[width, height];

    for (int x = 1; x < width - 1; x++)
    {
      resultMatrix[x, 0] = inputMatrix[x, 0];
      resultMatrix[x, height-1] = inputMatrix[x, height - 1];
    }

    for (int y = 1; y < height - 1; y++)
    {
      resultMatrix[0, y] = inputMatrix[0, y];
      resultMatrix[width-1, y] = inputMatrix[width - 1, y];
    }

    for (int y = 1; y < height - 1; y++)
    {
      for (int x = 1; x < width - 1; x++)
      {
        if(inputMatrix[x, y] < 1)
        {
          resultMatrix[x, y] = inputMatrix[x, y];
        }
        else if (inputMatrix[x, y] == setValue ||
            inputMatrix[x - 1, y - 1] == setValue || inputMatrix[x, y - 1] == setValue || inputMatrix[x + 1, y - 1] == setValue ||
            inputMatrix[x - 1, y] == setValue || inputMatrix[x, y] == setValue || inputMatrix[x + 1, y] == setValue ||
            inputMatrix[x - 1, y + 1] == setValue || inputMatrix[x, y + 1] == setValue || inputMatrix[x + 1, y + 1] == setValue)
        {
          resultMatrix[x, y] = Math.Min(setValue + newValue, 1);
        }
        else
        {
          resultMatrix[x, y] = 1;
        }
      }
    }

    return resultMatrix;
  }

  static double[,] Dilation (double[,] inputMatrix, int width, int height, double setValue, double newValue)
  {
    double[,] resultMatrix = new double[width, height];

    for (int x = 1; x < width - 1; x++)
    {
      resultMatrix[x, 0] = inputMatrix[x, 0];
      resultMatrix[x, height - 1] = inputMatrix[x, height - 1];
    }

    for (int y = 1; y < height - 1; y++)
    {
      resultMatrix[0, y] = inputMatrix[0, y];
      resultMatrix[width - 1, y] = inputMatrix[width - 1, y];
    }

    for (int y = 1; y < height - 1; y++)
    {
      for (int x = 1; x < width - 1; x++)
      {
        if (inputMatrix[x, y] != 0)
        {
          resultMatrix[x, y] = inputMatrix[x, y];
        }
        else if (
            inputMatrix[x - 1, y - 1] == setValue || inputMatrix[x, y - 1] == setValue || inputMatrix[x + 1, y - 1] == setValue ||
            inputMatrix[x - 1, y] == setValue || inputMatrix[x + 1, y] == setValue ||
            inputMatrix[x - 1, y + 1] == setValue || inputMatrix[x, y + 1] == setValue || inputMatrix[x + 1, y + 1] == setValue)
        {
          resultMatrix[x, y] = Math.Max(setValue-newValue,0);
        }
        else
        {
          resultMatrix[x, y] = 0;
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
    bool ycbcrCheck = (ycbcr.Y >= 0 && ycbcr.Y <= 255) && (ycbcr.Cb >= 85 && ycbcr.Cb <= 135) && (ycbcr.Cr >= 135 && ycbcr.Cr <= 180);
    return hsvCheck||ycbcrCheck;
  }

  public bool IsSkinPixel2 (Hsv hsv, YCbCr ycbcr)
  {
    // Skin tone thresholds in HSV and YCbCr color spaces
    bool hsvCheck = (hsv.H >= 0 && hsv.H <= 35) && (hsv.S >= 0.05882 && hsv.S <= 0.666667) && (hsv.V >= 0 && hsv.V <= 1);
    bool ycbcrCheck = (ycbcr.Y >= 0 && ycbcr.Y <= 255) && (ycbcr.Cr >= 135 && ycbcr.Cr <= 180) && (ycbcr.Cb >= 85 && ycbcr.Cb <= 135);
    return hsvCheck && ycbcrCheck;
  }

  public bool IsAlmostSkinPixel (CieLab labColor)
  {
    // Skin tone thresholds in HSV and YCbCr color spaces
    bool labCheck = (labColor.L >= 30 && labColor.L <= 100) && (labColor.A >= -10 && labColor.A <= 45) && (labColor.B >= -10 && labColor.B <= 45) &&  (Math.Abs(labColor.A - labColor.B) > 10 || (Math.Abs(labColor.A) < 10 && Math.Abs(labColor.B) < 10)) ;

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

using System;
using System.IO;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace CreateAllRGBColorsImage
{
  public class CircleDrawer
  {
    int maxValue = 16777216;
    int currentIndex=0;
    List<int> colorList;
    public CircleDrawer ()
    {
      colorList = new List<int>(maxValue);
      // Fill the list with the desired value (0) initially.
      for (int i = 0; i < maxValue; i++)
      {
        colorList.Add(i);
      }
    }

    public Rgba32 nextColor ()
    {
      Rgba32 result;

      int ColorInt = colorList[currentIndex];
      int r = (ColorInt & 0xFF);
      int g = ((ColorInt >> 8) & 0xFF);
      int b = ((ColorInt >> 16) & 0xFF);

      // Modify the components to generate a new color.
      // You can adjust these values to control how "close" the colors should be.
      r = (r) % 256;  
      g = (g) % 256;   
      b = (b) % 256;   
      result = new Rgba32((byte)r, (byte)g, (byte)b);

      // Initialize the previous color if it's the first call.
      if ((currentIndex+1) < colorList.Count)
      {
        currentIndex++;
      }
      else
      {
        currentIndex = 0;
      }

      return result;
    }

    public bool PixelHasAssignedColor (Image<Rgba32> image, int x, int y)
    {
      if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
      {
        Rgba32 pixelColor = image[x, y];
        return pixelColor.A != 0; // Check if the alpha component is not 0 (i.e., not transparent)
      }
      return false; // Pixel is out of image bounds
    }

    private bool IsInsideImage (int width, int height, int x, int y)
    {
      return x >= 0 && x < width && y >= 0 && y < height;
    }

    public void DrawEmptySquares (Image<Rgba32> image, int x, int y, int size, int width)
    {
      int halfSize = size / 2;

      for (int w = 0; w < width; w++)
      {
        int left = x - halfSize + w;
        int right = x + halfSize - w;
        int top = y - halfSize + w;
        int bottom = y + halfSize - w;

        // Draw the top border
        for (int i = left; i <= right; i++)
        {
          if (i >= 0 && i < image.Width)
          {
            if (IsInsideImage(image.Width, image.Height, i, top) && !PixelHasAssignedColor(image, i, top))
            {
              image[i, top] = nextColor();
            }
          }
        }

        // Draw the right border
        for (int j = top; j <= bottom; j++)
        {
          if (j >= 0 && j < image.Height)
          {
            if (IsInsideImage(image.Width, image.Height, right, j) && !PixelHasAssignedColor(image, right, j))
            {
              image[right, j] = nextColor();
            }
          }
        }

        // Draw the bottom border
        for (int i = right; i >= left ; i--)
        {
          if (i >= 0 && i < image.Width)
          {
            if (IsInsideImage(image.Width, image.Height, i, bottom) && !PixelHasAssignedColor(image, i, bottom))
            {
              image[i, bottom] = nextColor();
            }
          }
        }

        // Draw the left border
        for (int j = bottom; j >= top; j--)
        {
          if (j >= 0 && j < image.Height)
          {
            if (IsInsideImage(image.Width, image.Height, left, j) && !PixelHasAssignedColor(image, left, j))
            {
              image[left, j] = nextColor();
            }
          }
        }
      }
    }

    public void DrawConcentricCircles (Image<Rgba32> image, int xc, int yc, int radius)
    {
      for (int i = 0; i <= radius; i++)
      {
        DrawCircle(image, xc, yc, i);
      }
    }

    public void DrawEmptyCircles (Image<Rgba32> image, int xc, int yc, int radius, int width)
    {
      for (int i = (radius-width); i <= radius; i++)
      {
        DrawCircle(image, xc, yc, i);
      }
    }

    public void DrawCircle (Image<Rgba32> image, int xc, int yc, int r)
    {
      {
        int x = 0, y = r;
        int d = 3 - 2 * r;
        DrawCirclePixels(image, xc, yc, x, y);

        while (y >= x)
        {
          x++;

          if (d > 0)
          {
            y--;
            d += 4 * (x - y) + 10;
          }
          else
          {
            d += 4 * x + 6;
          }

          DrawCirclePixels(image, xc, yc, x, y);

          // Draw a slightly larger circle to eliminate gaps
          DrawCirclePixels(image, xc, yc, x, y + 1);
        }
      }
    }

    public void DrawCirclePixels (Image<Rgba32> image, int xc, int yc, int x, int y)
    {
      int imageWidth = image.Width;
      int imageHeight = image.Height;

      for (int dx = -1; dx <= 1; dx += 2)
      {
        for (int dy = -1; dy <= 1; dy += 2)
        {
          DrawPixelIfValid(image, imageWidth, imageHeight, xc + dx * x, yc + dy * y);
          DrawPixelIfValid(image, imageWidth, imageHeight, xc + dx * y, yc + dy * x);
        }
      }
    }

    private void DrawPixelIfValid (Image<Rgba32> image, int imageWidth, int imageHeight, int x, int y)
    {
      if (IsInsideImage(imageWidth, imageHeight, x, y) && !PixelHasAssignedColor(image, x, y))
      {
        image[x, y] = this.nextColor();
      }
    }


    public void CreateEquallyDistributedCircles (Image<Rgba32> image, int centerX, int centerY, int radius, int numberOfCircles, int distance)
    {
      for (int i = 0; i < numberOfCircles; i++)
      {
        double angle = 2 * Math.PI * i / numberOfCircles; // Calculate the angle for the current circle

        // Calculate the coordinates for the center of the current circle
        int x = centerX + (int)(distance * Math.Cos(angle));
        int y = centerY + (int)(distance * Math.Sin(angle));

        // Draw the circle at the calculated coordinates
        DrawConcentricCircles(image, x, y, radius);
      }
    }

    public void FindUnassignedPixels (Image<Rgba32> image)
    {
      int width = image.Width;
      int height = image.Height;

      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          Rgba32 pixel = image[x, y];
          if (pixel == default(Rgba32))
          {
            Console.WriteLine($"Unassigned pixel at ({x}, {y})");
          }
        }
      }
    }

    public void FindMissingColors (Image<Rgba32> image)
    {
      int width = image.Width;
      int height = image.Height;

      // Create a HashSet to store the colors present in the image
      var colorsPresent = new HashSet<Rgba32>();

      // Iterate through the image and add all unique colors to the HashSet
      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          Rgba32 pixel = image[x, y];
          colorsPresent.Add(pixel);
        }
      }

      // Iterate through all possible color values and check if they are missing
      for (int r = 0; r <= 255; r++)
      {
        for (int g = 0; g <= 255; g++)
        {
          for (int b = 0; b <= 255; b++)
          {
            Rgba32 colorToCheck = new Rgba32((byte)r, (byte)g, (byte)b);

            if (!colorsPresent.Contains(colorToCheck))
            {
              Console.WriteLine($"Missing color: {colorToCheck}");
            }
          }
        }
      }
    }
  }
    internal class Program
  {

    static void Main (string[] args)
    {
      // Check if the correct number of arguments is provided
      if (args.Length < 4)
      {
        Console.WriteLine("Usage: CreateAllRGBColorsImage <width> <height> <output_filename> <mode> [seed]");
        Console.WriteLine("Modes: trivial, random, pattern, mandala");
        return;
      }

      // Parse command-line arguments
      int width = int.Parse(args[0]);
      int height = int.Parse(args[1]);
      string outputFilename = args[2];
      string mode = args[3];
      int? seed = args.Length > 4 ? int.Parse(args[4]) : null;

      // Check if the image size is smaller than 16M pixels
      if (width * height < 16777216)
      {
        Console.WriteLine("Image must have at least 16 777 216 pixels.");
        return;
      }

      // Create a new image with the specified dimensions
      using (var image = new Image<Rgba32>(width, height))
      {
        switch (mode.ToLower())
        {
          case "trivial":
            GenerateTrivialImage(image);
            break;
          case "random":
            GenerateRandomImage(image, seed);
            break;
          case "pattern":
            GeneratePatternImage(image);
            break;
          case "mandala":
            GenerateMandalaImage(image);
            break;
          default:
            Console.WriteLine("Invalid mode. Available modes: trivial, random, pattern, mandala");
            return;
        }

        // Save the image to the specified filename
        image.Save(outputFilename);

        Console.WriteLine($"Image '{outputFilename}' created successfully in {mode} mode.");
      }
    }

    // Generate a trivial image with all RGB colors
    private static void GenerateTrivialImage (Image<Rgba32> image)
    {
      int colorInt = 0;

      for (int y = 0; y < image.Height; y++)
      {
        for (int x = 0; x < image.Width; x++)
        {
          byte r = (byte)(colorInt & 0xFF);
          byte g = (byte)((colorInt >> 8) & 0xFF);
          byte b = (byte)((colorInt >> 16) & 0xFF);
          image[x, y] = new Rgba32(r, g, b);
          colorInt++;
        }
      }
    }


    // Generate a random image with all RGB colors
    private static void GenerateRandomImage (Image<Rgba32> image, int? seed)
    {
      int maxValue = 16777216;
      List<int> colorList = new List<int>(maxValue);

      // Fill the list with the desired value (0) initially.
      for (int i = 0; i < maxValue; i++)
      {
        colorList.Add(i);
      }

      Shuffle(colorList, seed);

      int index = 0;

      for (int y = 0; y < image.Height; y++)
      {
        for (int x = 0; x < image.Width; x++)
        {
          // Find a random unused color

          // Save the randomly selected element in a variable
          int colorInt = colorList[index];
          index++;
          if (index == colorList.Count)
          {
            index = 0;
          }

          byte r = (byte)(colorInt & 0xFF);
          byte g = (byte)((colorInt >> 8) & 0xFF);
          byte b = (byte)((colorInt >> 16) & 0xFF);
          image[x, y] = new Rgba32(r, g, b);
        }
      }
    }

    public static void Shuffle (List<int> list, int? seed)
    {
      Random random;

      if (seed == null)
      {
        random = new Random();
      }
      else
      {
        random = new Random(seed.Value);
      }
      int n = list.Count;
      for (int i = n - 1; i > 0; i--)
      {
        int j = random.Next(0, i + 1); // Generate a random index between 0 and i (inclusive).
                                       // Swap the elements at i and j.
        int temp = list[i];
        list[i] = list[j];
        list[j] = temp;
      }
    }

    // Generate an ornament or pattern image with all RGB colors
    private static void GeneratePatternImage (Image<Rgba32> image)
    {
      CircleDrawer drawer = new CircleDrawer();
      long width = (long)image.Width;
      long height = (long)image.Height;
      int radius = (int)Math.Sqrt(width * width + height * height) + 1;
      drawer.DrawConcentricCircles(image, image.Width / 2, image.Height / 2, radius);
    }

    private static void GenerateMandalaImage (Image<Rgba32> image)
    {
      CircleDrawer drawer = new CircleDrawer();

      Console.WriteLine("Enter command e.g.:");
      Console.WriteLine("       'fullCircle' x y radius");
      Console.WriteLine("       'circleEdge' x y radius width");
      Console.WriteLine("       'multiCircles' x y radius numberOfCircles distanceFromCenter");
      Console.WriteLine("       'square' x y sideSize width");
      Console.WriteLine("       'exit' to quit and save");

      while (true)
      {
        string input = Console.ReadLine();

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
          break; // Exit the loop if the user types 'exit'
        }

        string[] parts = input.Split(' ');

        if (parts.Length >= 4 && parts[0].Equals("fullCircle", StringComparison.OrdinalIgnoreCase))
        {
          DrawFullCircle(image, drawer, parts);
        }
        else if (parts.Length == 5 && parts[0].Equals("circleEdge", StringComparison.OrdinalIgnoreCase))
        {
          DrawCircleEdge(image, drawer, parts);
        }
        else if (parts.Length == 5 && parts[0].Equals("square", StringComparison.OrdinalIgnoreCase))
        {
          DrawSquares(image, drawer, parts);
        }
        else if (parts.Length == 6 && parts[0].Equals("multiCircles", StringComparison.OrdinalIgnoreCase))
        {
          DrawMultiCircles(image, drawer, parts);
        }
        else
        {
          Console.WriteLine("## Invalid command ##");
          Console.WriteLine("");
        }
      }
    }

    private static void DrawFullCircle (Image<Rgba32> image, CircleDrawer drawer, string[] parts)
    {
      int x, y, radius;

      if (parts[1].Equals("center", StringComparison.OrdinalIgnoreCase))
      {
        x = image.Width / 2;
      }
      else if (!int.TryParse(parts[1], out x))
      {
        Console.WriteLine("Invalid input for x. Please provide a valid number or 'center'.");
        return;
      }

      if (parts[2].Equals("center", StringComparison.OrdinalIgnoreCase))
      {
        y = image.Height / 2;
      }
      else if (!int.TryParse(parts[2], out y))
      {
        Console.WriteLine("Invalid input for y. Please provide a valid number or 'center'.");
        return;
      }

      if (parts[3].Equals("max", StringComparison.OrdinalIgnoreCase))
      {
        int maxX = Math.Max(x, image.Width - x);
        int maxY = Math.Max(y, image.Height - y);
        radius = (int)Math.Sqrt(maxX * maxX + maxY * maxY) + 1;
      }
      else if (!int.TryParse(parts[3], out radius))
      {
        Console.WriteLine("Invalid input for radius. Please provide a valid number or 'max'.");
        return;
      }

      drawer.DrawConcentricCircles(image, x, y, radius);
      Console.WriteLine("-- Full Circle Added --");
      Console.WriteLine("");
    }

    private static void DrawCircleEdge (Image<Rgba32> image, CircleDrawer drawer, string[] parts)
    {
      int x, y, radius, width;

      if (parts[1].Equals("center", StringComparison.OrdinalIgnoreCase))
      {
        x = image.Width / 2;
      }
      else if (!int.TryParse(parts[1], out x))
      {
        Console.WriteLine("Invalid input for x. Please provide a valid number or 'center'.");
        return;
      }

      if (parts[2].Equals("center", StringComparison.OrdinalIgnoreCase))
      {
        y = image.Height / 2;
      }
      else if (!int.TryParse(parts[2], out y))
      {
        Console.WriteLine("Invalid input for y. Please provide a valid number or 'center'.");
        return;
      }

      if (parts[3].Equals("max", StringComparison.OrdinalIgnoreCase))
      {
        int maxX = Math.Max(x, image.Width - x);
        int maxY = Math.Max(y, image.Height - y);
        radius = (int)Math.Sqrt(maxX * maxX + maxY * maxY) + 1;
      }
      else if (!int.TryParse(parts[3], out radius))
      {
        Console.WriteLine("Invalid input for radius. Please provide a valid number or 'max'.");
        return;
      }

      if (!int.TryParse(parts[4], out width))
      {
        Console.WriteLine("Invalid input for width. Please provide a valid number.");
        return;
      }

      drawer.DrawEmptyCircles(image, x, y, radius, width);
      Console.WriteLine("-- Circle Edge Added --");
      Console.WriteLine("");
    }

    private static void DrawMultiCircles (Image<Rgba32> image, CircleDrawer drawer, string[] parts)
    {
      int x, y, radius, numberOfCircles, distance;

      if (parts[1].Equals("center", StringComparison.OrdinalIgnoreCase))
      {
        x = image.Width / 2;
      }
      else if (!int.TryParse(parts[1], out x))
      {
        Console.WriteLine("Invalid input for x. Please provide a valid number or 'center'.");
        return;
      }

      if (parts[2].Equals("center", StringComparison.OrdinalIgnoreCase))
      {
        y = image.Height / 2;
      }
      else if (!int.TryParse(parts[2], out y))
      {
        Console.WriteLine("Invalid input for y. Please provide a valid number or 'center'.");
        return;
      }

      if (!int.TryParse(parts[3], out radius))
      {
        Console.WriteLine("Invalid input for radius. Please provide a valid number or 'max'.");
        return;
      }

      if (!int.TryParse(parts[4], out numberOfCircles))
      {
        Console.WriteLine("Invalid input for numberOfCircles. Please provide a valid number.");
        return;
      }

      if (parts[5].Equals("max", StringComparison.OrdinalIgnoreCase))
      {
        int maxX = Math.Max(x, image.Width - x);
        int maxY = Math.Max(y, image.Height - y);
        distance = (int)Math.Sqrt(maxX * maxX + maxY * maxY);
      }
      else if (!int.TryParse(parts[5], out distance))
      {
        Console.WriteLine("Invalid input for distance. Please provide a valid number or 'max'.");
        return;
      }

      drawer.CreateEquallyDistributedCircles(image, x, y, radius, numberOfCircles, distance);
      Console.WriteLine("-- Circles Added --");
      Console.WriteLine("");
    }

    private static void DrawSquares (Image<Rgba32> image, CircleDrawer drawer, string[] parts)
    {
      int x, y, size, width;

      if (parts[1].Equals("center", StringComparison.OrdinalIgnoreCase))
      {
        x = image.Width / 2;
      }
      else if (!int.TryParse(parts[1], out x))
      {
        Console.WriteLine("Invalid input for x. Please provide a valid number or 'center'.");
        return;
      }

      if (parts[2].Equals("center", StringComparison.OrdinalIgnoreCase))
      {
        y = image.Height / 2;
      }
      else if (!int.TryParse(parts[2], out y))
      {
        Console.WriteLine("Invalid input for y. Please provide a valid number or 'center'.");
        return;
      }

      if (parts[3].Equals("max", StringComparison.OrdinalIgnoreCase))
      {
        size = Math.Max(image.Width, image.Height);
      }
      else if (!int.TryParse(parts[3], out size))
      {
        Console.WriteLine("Invalid input for sideSize. Please provide a valid number or 'max'.");
        return;
      }

      if (parts[4].Equals("max", StringComparison.OrdinalIgnoreCase))
      {
        width = (Math.Max(image.Width, image.Height)/2)+1;
      }
      else if(!int.TryParse(parts[4], out width))
      {
        Console.WriteLine("Invalid input for width. Please provide a valid number.");
        return;
      }

      drawer.DrawEmptySquares(image, x, y, size, width);
      Console.WriteLine("-- Square Added --");
      Console.WriteLine("");
    }
  }
}

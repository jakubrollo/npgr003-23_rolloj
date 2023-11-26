using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using CommandLine;
using System.Net;
using System.Security.AccessControl;


public class Options
{
  [Option('o', "output", Required = true,  HelpText = "Output file for the mandala.")]
  public string OutputFile { get; set; }

  [Option('w', "width", Required = false, HelpText = "Width of output file.")]
  public int? Width { get; set; }

  [Option('h', "height", Required = false, HelpText = "Height of output file.")]
  public int? Height { get; set; }
}

class FlowerDrawer
{
  public static void DrawMultiLayeredFlower (Image<Rgba32> image, SizeF radius, int petalLength, int numberOfPetals, int distance, Color color1, Color color2, int numberOfLayers = 1, int angleOffset = 0)
  {
    int smallerPetalLength = petalLength /numberOfLayers;
    int layerPetalLength = petalLength;
    int movedRotation = angleOffset;
    float radius2 = radius.Height/numberOfLayers;
    float radius1 = radius.Width/numberOfLayers;
    SizeF smallerRadius = radius;
    Color flowerColor = color1;
    for (int layer = numberOfLayers; layer > 0; layer--)
    {
      DrawFlower(image, smallerRadius, layerPetalLength, numberOfPetals, flowerColor, movedRotation, distance);
      if (movedRotation == angleOffset)
      {
        movedRotation = ((360 / numberOfPetals) / 2) + angleOffset;
        flowerColor = color1;
      }
      else
      {
        flowerColor = color2;
        movedRotation = angleOffset;
      }
      smallerRadius.Height = smallerRadius.Height - radius2;
      smallerRadius.Width = smallerRadius.Width - radius1;
      layerPetalLength = layerPetalLength - smallerPetalLength;
    }
  }
  public static void DrawFlower (Image<Rgba32> image, SizeF radius, int petalLength, int numberOfPetals, Color flowerColor, int startRotation = 0, int distance = 0)
  {
    for (int i = 0; i < numberOfPetals; i++)
    {
      float angle = (i * (360/numberOfPetals)) + startRotation; 

      DrawPetal(image, radius, angle, petalLength, distance, flowerColor);
    }
  }
  public static void DrawPetal (Image<Rgba32> image, SizeF radius, float angle, int petalLength, int distance, Color flowerColor)
  {
    PointF center = new PointF(image.Width / 2, image.Height / 2);
    PointF startPoint = new PointF(
          center.X + (float)(distance * Math.Cos(angle * Math.PI / 180.0)),
          center.Y + (float)(distance * Math.Sin(angle * Math.PI / 180.0))
      );

    // Calculate the end point based on the angle and radius
    PointF endPoint = new PointF(
          startPoint.X + (float)(petalLength * Math.Cos(angle * Math.PI / 180.0)),
          startPoint.Y + (float)(petalLength * Math.Sin(angle * Math.PI / 180.0))
     );

    // Create the ArcLineSegment
    ArcLineSegment petal = new ArcLineSegment(startPoint, endPoint, radius, angle, false, true);
    ArcLineSegment petal2 = new ArcLineSegment(endPoint, startPoint, radius, angle, false, true);

    Pen pen = Pens.Solid(Color.Black, 5);
    // Draw the petal using PathBuilder and the specified constructor
    PathBuilder pathBuilder = new PathBuilder();
    pathBuilder.AddSegment(petal);
    pathBuilder.AddSegment(petal2);
    DrawingOptions options = new();
    image.Mutate(x => x.Fill(options, flowerColor, pathBuilder.Build())
                      .Draw(pen, pathBuilder.Build()));
  }

  public static void DrawPetalCircles (Image<Rgba32> image, SizeF radius, float angle)
  {
    PointF center = new PointF(image.Width / 2, image.Height / 2);

    PointF endPoint = new PointF(
          center.X + radius.Width * (float)Math.Cos(MathF.PI * angle / 180),
          center.Y + radius.Height * (float)Math.Sin(MathF.PI * angle / 180)
      );

    ArcLineSegment petal = new ArcLineSegment(center, endPoint, radius, 0, true, true);
    ArcLineSegment petal2 = new ArcLineSegment(endPoint, center, radius, 0, true, true);


    Pen pen = Pens.Solid(Color.Black, 5);
    PathBuilder pathBuilder = new PathBuilder();
    pathBuilder.AddSegment(petal);

    image.Mutate(x => x.Draw(pen, pathBuilder.Build()));
    pathBuilder.Clear();
    pathBuilder.AddSegment(petal2);
    image.Mutate(x => x.Draw(pen, pathBuilder.Build()));

  }

  public static void DrawRainDropCircle (Image<Rgba32> image, int length, int width, int numberOfRainDrops, Color givenColor, int startRotation = 0, int distance = 0)
  {
    for (int i = 0; i < numberOfRainDrops; i++)
    {
      float angle = (i * (360/numberOfRainDrops)) + startRotation; // Adjust the angle based on the number of petals

      DrawRainDrop(image, length, width, givenColor, angle, distance);
    }
  }

  public static void DrawRainDrop (Image<Rgba32> image, int length, int width, Color givenColor, float rotation = 0, int distance = 0)
  {
    double hypotenuse = Math.Sqrt(length*length + width*width);
    SizeF radius= new SizeF((float)(2.87*hypotenuse), (float)(2.7788*hypotenuse));
    int halfWidth = width / 2;
    PointF center = new PointF(image.Width / 2, image.Height / 2);

    PointF startPoint = new PointF(
          center.X + (float)((distance-halfWidth) * Math.Cos((90+rotation) * Math.PI / 180.0)),
          center.Y + (float)((distance-halfWidth) * Math.Sin((90+rotation) * Math.PI / 180.0))
      );

    PointF startPoint1 = new PointF(
          startPoint.X + (float)(halfWidth * Math.Cos((0+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(halfWidth * Math.Sin((0+rotation) * Math.PI / 180.0))
      );

    PointF startPoint2 = new PointF(
          startPoint.X + (float)(halfWidth * Math.Cos((180+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(halfWidth * Math.Sin((180+rotation) * Math.PI / 180.0))
      );

    PointF endPoint = new PointF(
          startPoint.X + (float)((length-halfWidth) * Math.Cos((90+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)((length-halfWidth) * Math.Sin((90+rotation) * Math.PI / 180.0))
     );

    ArcLineSegment petal = new ArcLineSegment(startPoint1, endPoint, radius, 90, false, true);
    ArcLineSegment petal2 = new ArcLineSegment(endPoint, startPoint2, radius, 90, false, true);
    ArcLineSegment petal3 = new ArcLineSegment(startPoint2, startPoint1,  new SizeF(halfWidth, halfWidth), 90, false, true);

    Pen pen = Pens.Solid(Color.Black, 5);
    PathBuilder pathBuilder = new PathBuilder();
    pathBuilder.AddSegment(petal);
    pathBuilder.AddSegment(petal2);
    pathBuilder.AddSegment(petal3);
    DrawingOptions options = new();
    image.Mutate(x => x.Fill(options, givenColor, pathBuilder.Build())
                      .Draw(pen, pathBuilder.Build()));
  }


  public static void DrawRainDrop2Circle (Image<Rgba32> image, int length, int width, int numberOfRainDrops, int startRotation = 0, int distance = 0)
  {
    for (int i = 0; i < numberOfRainDrops; i++)
    {
      float angle = (i * (360/numberOfRainDrops)) + startRotation;

      DrawRainDrop2(image, length, width, angle, distance);
    }
  }

  public static void DrawRainDrop2 (Image<Rgba32> image, int length, int width, float rotation = 0, int distance = 0)
  {

    double hypotenuse = Math.Sqrt(length*length + width*width);
    SizeF radius= new SizeF((float)(2.87*hypotenuse), (float)(2.7788*hypotenuse));
    int halfWidth = width / 2;
    PointF center = new PointF(image.Width / 2, image.Height / 2);

    PointF startPoint = new PointF(
          center.X + (float)(distance * Math.Cos((270+rotation) * Math.PI / 180.0)),
          center.Y + (float)(distance * Math.Sin((270+rotation) * Math.PI / 180.0))
      );

    PointF startPoint1 = new PointF(
          startPoint.X + (float)(halfWidth * Math.Cos((0+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(halfWidth * Math.Sin((0+rotation) * Math.PI / 180.0))
      );

    PointF startPoint2 = new PointF(
          startPoint.X + (float)(halfWidth * Math.Cos((180+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(halfWidth * Math.Sin((180+rotation) * Math.PI / 180.0))
      );

    PointF endPoint = new PointF(
          startPoint.X + (float)(length * Math.Cos((270+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(length * Math.Sin((270+rotation) * Math.PI / 180.0))
     );

    ArcLineSegment petal = new ArcLineSegment(startPoint1, endPoint, radius, 270, false, true);
    ArcLineSegment petal2 = new ArcLineSegment(endPoint, startPoint2, radius, 270, false, true);
    ArcLineSegment petal3 = new ArcLineSegment(startPoint1, startPoint2, new SizeF(halfWidth, halfWidth), 90, false, true);

    Pen pen = Pens.Solid(Color.Black, 5);
    PathBuilder pathBuilder = new PathBuilder();
    pathBuilder.AddSegment(petal);
    image.Mutate(x => x.Draw(pen, pathBuilder.Build()));
    pathBuilder.Clear();
    pathBuilder.AddSegment(petal2);
    image.Mutate(x => x.Draw(pen, pathBuilder.Build()));
    pathBuilder.Clear();
    pathBuilder.AddSegment(petal3);
    image.Mutate(x => x.Draw(pen, pathBuilder.Build()));
  }

  public static void DrawInvertedRainDropCircle (Image<Rgba32> image, int length, int width, int numberOfRainDrops, int startRotation = 0, int distance = 0)
  {
    for (int i = 0; i < numberOfRainDrops; i++)
    {
      float angle = (i * (360/numberOfRainDrops)) + startRotation;

      DrawInvertedRainDrop(image, length, width, angle, distance);
    }
  }

  public static void DrawInvertedRainDrop (Image<Rgba32> image, int length, int width, float rotation = 0, int distance = 0)
  {
    double hypotenuse = Math.Sqrt(length*length + width*width);
    SizeF radius= new SizeF((float)(2.87*hypotenuse), (float)(2.7788*hypotenuse));
    int halfWidth = width / 2;
    PointF center = new PointF(image.Width / 2, image.Height / 2);

    PointF startPoint = new PointF(
          center.X + (float)(distance * Math.Cos((270+rotation) * Math.PI / 180.0)),
          center.Y + (float)(distance * Math.Sin((270+rotation) * Math.PI / 180.0))
      );

    PointF startPoint1 = new PointF(
          startPoint.X + (float)(halfWidth * Math.Cos((0+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(halfWidth * Math.Sin((0+rotation) * Math.PI / 180.0))
      );

    PointF startPoint2 = new PointF(
          startPoint.X + (float)(halfWidth * Math.Cos((180+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(halfWidth * Math.Sin((180+rotation) * Math.PI / 180.0))
      );

    PointF endPoint = new PointF(
          startPoint.X + (float)(length * Math.Cos((90+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(length * Math.Sin((90+rotation) * Math.PI / 180.0))
     );

    ArcLineSegment petal = new ArcLineSegment(startPoint1, endPoint, radius, 270, false, true);
    ArcLineSegment petal2 = new ArcLineSegment(endPoint, startPoint2, radius, 270, false, true);
    ArcLineSegment petal3 = new ArcLineSegment(startPoint2, startPoint1, new SizeF(halfWidth, halfWidth), 90, true, false);

    Pen pen = Pens.Solid(Color.Black, 5);
    PathBuilder pathBuilder = new PathBuilder();
    pathBuilder.AddSegment(petal);
    pathBuilder.AddSegment(petal2);
    pathBuilder.AddSegment(petal3);
    DrawingOptions options = new();
    image.Mutate(x => x.Fill(options, SixLabors.ImageSharp.Color.Red, pathBuilder.Build())
                      .Draw(pen, pathBuilder.Build()));
  }

  public static void DrawRotatedRainDropCircle (Image<Rgba32> image, int length, int width, int numberOfRainDrops, Color color, int startRotation = 0, int distance = 0)
  {
    for (int i = 0; i < numberOfRainDrops; i++)
    {
      float angle = (i * (360/numberOfRainDrops)) + startRotation;

      DrawRotatedRainDrop(image, length, width, color, angle, distance);
    }
  }

  public static void DrawRotatedRainDrop (Image<Rgba32> image, int length, int width, Color color, float rotation = 0, int distance = 0)
  {
    double hypotenuse = Math.Sqrt(length*length + width*width);
    SizeF radius= new SizeF((float)(2.87*hypotenuse), (float)(2.7788*hypotenuse));
    int halfWidth = width / 2;
    PointF center = new PointF(image.Width / 2, image.Height / 2);

    PointF startPoint = new PointF(
          center.X + (float)((distance+length-halfWidth) * Math.Cos((270+rotation) * Math.PI / 180.0)),
          center.Y + (float)((distance+length-halfWidth) * Math.Sin((270+rotation) * Math.PI / 180.0))
      );

    PointF startPoint1 = new PointF(
          startPoint.X + (float)(halfWidth * Math.Cos((0+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(halfWidth * Math.Sin((0+rotation) * Math.PI / 180.0))
      );

    PointF startPoint2 = new PointF(
          startPoint.X + (float)(halfWidth * Math.Cos((180+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)(halfWidth * Math.Sin((180+rotation) * Math.PI / 180.0))
      );

    PointF endPoint = new PointF(
          startPoint.X + (float)((length-halfWidth) * Math.Cos((90+rotation) * Math.PI / 180.0)),
          startPoint.Y + (float)((length-halfWidth) * Math.Sin((90+rotation) * Math.PI / 180.0))
     );

    ArcLineSegment petal = new ArcLineSegment(startPoint1, endPoint, radius, 270, false, true);
    ArcLineSegment petal2 = new ArcLineSegment(endPoint, startPoint2, radius, 270, false, true);
    ArcLineSegment petal3 = new ArcLineSegment(startPoint2, startPoint1, new SizeF(halfWidth, halfWidth), 90, false, true);

    Pen pen = Pens.Solid(Color.Black, 5);
    PathBuilder pathBuilder = new PathBuilder();
    pathBuilder.AddSegment(petal);
    pathBuilder.AddSegment(petal2);
    pathBuilder.AddSegment(petal3);
    DrawingOptions options = new();
    image.Mutate(x => x.Fill(options, color, pathBuilder.Build())
                      .Draw(pen, pathBuilder.Build()));
  }
  static float CalculateDistance (PointF point1, PointF point2)
  {
    float deltaX = point2.X - point1.X;
    float deltaY = point2.Y - point1.Y;

    float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    return distance;
  }
  public static void DrawFlowerOutline (Image<Rgba32> image, int numberOfPetals, float radius, Color color)
  {
    PointF center = new PointF(image.Width / 2, image.Height / 2);
    Pen pen = Pens.Solid(Color.Black, 5);
    PathBuilder pathBuilder = new PathBuilder();

    PointF firstPoint = new PointF(
          center.X + (float)(radius * Math.Cos((0) * Math.PI / 180.0)),
          center.Y + (float)(radius * Math.Sin((0) * Math.PI / 180.0))
    );
    PointF flowerPoint1 = firstPoint;
    float angle = 360/numberOfPetals;
    float distance = (float)(Math.Sqrt(2*(radius*radius) - (2*radius*radius*Math.Cos((angle) * Math.PI / 180.0))));
    ArcLineSegment petal;
    for (int i = 1; i < numberOfPetals; i++)
    {
      float currentAngle = (i * angle);

      PointF flowerPoint2 = new PointF(
          center.X + (float)(radius * Math.Cos((currentAngle) * Math.PI / 180.0)),
          center.Y + (float)(radius * Math.Sin((currentAngle) * Math.PI / 180.0))
      );
      petal = new ArcLineSegment(flowerPoint1, flowerPoint2, new SizeF(distance / 2, distance / 2), currentAngle, false, true);
      pathBuilder.AddSegment(petal);
      flowerPoint1 = flowerPoint2;
    }
    petal = new ArcLineSegment(flowerPoint1, firstPoint, new SizeF(distance / 2, distance / 2), 360 - angle, false, true);
    pathBuilder.AddSegment(petal);
    DrawingOptions options = new();
    image.Mutate(x => x.Fill(options, color, pathBuilder.Build())
                      .Draw(pen, pathBuilder.Build()));
  }
  public static void DrawStar (Image<Rgba32> image, SizeF radius)
  {
    Pen pen = Pens.Solid(Color.Black, 5);
    PointF center = new PointF(image.Width / 2, image.Height / 2);

    for (int i = 0; i < 100; i++)
    {
      float angle = i * 10;

      PointF endPoint = new PointF(
            center.X + radius.Width * (float)Math.Cos(MathF.PI * angle),
            center.Y + radius.Height * (float)Math.Sin(MathF.PI * angle)
        );

      ArcLineSegment petal = new ArcLineSegment(center, endPoint, radius, 0, false, true);

      PathBuilder pathBuilder = new PathBuilder();
      pathBuilder.AddSegment(petal);
      image.Mutate(x => x.Draw(pen, pathBuilder.Build()));
    }
  }
  public static void DrawPolygon (Image<Rgba32> image, int vertices, float radius, Color color)
  {
    Pen pen = Pens.Solid(Color.Black, 5);
    PointF center = new PointF(image.Width / 2, image.Height / 2);
    RegularPolygon polygon = new(center, vertices, radius);
    DrawingOptions options = new();
    image.Mutate(x => x.Fill(options, color, polygon)
                .Draw(pen, polygon));
  }

  public static void DrawMultipleCircles (Image<Rgba32> image, PointF center, int numberOfCircles, float radius, float distance, Color circleColor, int startRotation = 0)
  {
    Pen pen = Pens.Solid(Color.Black, 5);
    DrawingOptions options = new();
    for (int i = 0; i < numberOfCircles; i++)
    {
      float angle = (i * (360/numberOfCircles)) + startRotation; 
      PointF circlePoint = new PointF(
          center.X + (float)(distance * Math.Cos((angle) * Math.PI / 180.0)),
          center.Y + (float)(distance * Math.Sin((angle) * Math.PI / 180.0))
      );
      EllipsePolygon circle = new EllipsePolygon( circlePoint, radius);
      image.Mutate(x => x.Fill(options, circleColor, circle)
                  .Draw(pen, circle));
    }
  }
  public static void DrawMultipleCirclesInACircle (Image<Rgba32> image, int numberOfCircles, int numberOfSmallCircles, float radiusSmall, float distanceBig, float distanceSmall, Color circleColor, int startRotation = 0, int startRotation2 = 0)
  {
    PointF center = new PointF(image.Width / 2, image.Height / 2);
    Pen pen = Pens.Solid(Color.Black, 5);
    DrawingOptions options = new();
    for (int i = 0; i < numberOfCircles; i++)
    {
      float angle = (i * (360/numberOfCircles)) + startRotation; 
      PointF circlePoint = new PointF(
          center.X + (float)(distanceBig * Math.Cos((angle) * Math.PI / 180.0)),
          center.Y + (float)(distanceBig * Math.Sin((angle) * Math.PI / 180.0))
      );
      DrawMultipleCircles(image, circlePoint, numberOfSmallCircles, radiusSmall, distanceSmall, circleColor, startRotation2);
    }
  }
}

class MandalaLogic
{
  private static void DefaultMandala (Options argumentOptions)
  {
    int width;
    int height;

    if (argumentOptions.Width == null)
    {
      width = 2048;
    }
    else
    {
      width = argumentOptions.Width.Value;
    }

    if (argumentOptions.Width == null)
    {
      height = 2048;
    }
    else
    {
      height = argumentOptions.Height.Value;
    }

    using (var image = new Image<Rgba32>(width, height, SixLabors.ImageSharp.Color.White))
    {
      image.Mutate(i => i.Clear(Color.Transparent));
      int petalLength = 100;
      Pen pen = Pens.Solid(Color.Black, 5);
      PointF center = new PointF(image.Width / 2, image.Height / 2);
      DrawingOptions options = new();
      FlowerDrawer.DrawFlowerOutline(image, 12, 720, Color.Blue);
      FlowerDrawer.DrawFlowerOutline(image, 12, 500, Color.Yellow);
      FlowerDrawer.DrawMultipleCircles(image, center, 12, 100, 490, Color.AliceBlue, 15);
      FlowerDrawer.DrawMultipleCircles(image, center, 12, 40, 490, Color.Cornsilk, 15);
      FlowerDrawer.DrawMultipleCirclesInACircle(image, 12, 10, 5, 490, 45, Color.DarkRed, 15);
      FlowerDrawer.DrawMultipleCirclesInACircle(image, 12, 10, 5, 490, 65, Color.DarkRed, 15, 12);
      FlowerDrawer.DrawMultipleCirclesInACircle(image, 12, 10, 5, 490, 85, Color.DarkRed, 15, 24);
      FlowerDrawer.DrawMultiLayeredFlower(image, new SizeF(120, 300), 150, 12, 390, Color.DarkTurquoise, Color.DeepPink, 3);
      EllipsePolygon circle = new EllipsePolygon(center, 402);
      image.Mutate(x => x.Fill(options, SixLabors.ImageSharp.Color.Salmon, circle)
                  .Draw(pen, circle));
      circle = new EllipsePolygon(center, 298);
      image.Mutate(x => x.Fill(options, SixLabors.ImageSharp.Color.SandyBrown, circle)
                        .Draw(pen, circle));
      circle = new EllipsePolygon(center, 100);
      image.Mutate(x => x.Fill(options, SixLabors.ImageSharp.Color.RoyalBlue, circle)
                        .Draw(pen, circle));

      FlowerDrawer.DrawFlowerOutline(image, 4, 200, Color.Firebrick);
      FlowerDrawer.DrawPolygon(image, 8, 200, Color.Goldenrod);
      FlowerDrawer.DrawMultipleCircles(image, center, 30, 6, 210, Color.Cornsilk);
      FlowerDrawer.DrawRotatedRainDropCircle(image, petalLength, 40, 60, Color.YellowGreen, 0, 300);
      FlowerDrawer.DrawMultipleCircles(image, center, 60, 8, 370, Color.WhiteSmoke);
      FlowerDrawer.DrawRainDropCircle(image, 300, 100, 8, Color.Linen, 45, 210);
      FlowerDrawer.DrawRainDropCircle(image, 200, 60, 8, Color.RoyalBlue, 45, 220);
      SizeF radius = new SizeF(120, 300);
      FlowerDrawer.DrawMultiLayeredFlower(image, radius, petalLength, 6, 0, Color.DarkTurquoise, Color.DeepPink, 3);
      FlowerDrawer.DrawMultiLayeredFlower(image, radius, petalLength, 6, 100, Color.Green, Color.Yellow, 3, 30);
      //FlowerDrawer.DrawFlowerOutline(image, radius, Number of petals, 6, 100, Color.Green, Color.Yellow, 3, 30);
      FlowerDrawer.DrawMultipleCirclesInACircle(image, 12, 10, 7, 490, 20, Color.DarkRed);
      FlowerDrawer.DrawInvertedRainDropCircle(image, 300, 200, 8, 0, 800);
      image.Save(argumentOptions.OutputFile);
    }
  }

  public static void MandalaHandler (Options argumentOptions)
  {
    DefaultMandala(argumentOptions);
  }
}

class Program
{
  static void Main (string[] args)
  {
    Parser.Default.ParseArguments<Options>(args)
    .WithParsed(options => MandalaLogic.MandalaHandler(options));
  }
}

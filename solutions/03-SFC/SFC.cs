using System.Diagnostics;

namespace _03_SFC;

// Callback function for the Pass() functions.
using PixelAction = Action<int, int>;

/// <summary>
/// API for the pixel orders.
/// </summary>
public interface IPixelOrder
{
  /// <summary>
  /// Passes all pixels from the rectangle [0,0]-[width-1,height-1]
  /// </summary>
  /// <param name="width">Rectangle width.</param>
  /// <param name="height">Rectangle height.</param>
  void Pass(int width, int height);
}

/// <summary>
/// Support class - inherit your own methods from it.
/// </summary>
public abstract class DefaultPixelOrder : IPixelOrder
{
  /// <summary>
  /// Function called for every pixel.
  /// </summary>
  protected PixelAction Callback;

  /// <summary>
  /// Constructors will be used for callback function definitions.
  /// </summary>
  /// <param name="callback">Callback function.</param>
  protected DefaultPixelOrder(PixelAction callback) => Callback = callback;

  /// <summary>
  /// Passes the given rectangle, calls the Callback() function for every pixel.
  /// The Callback() function should be called exactly width * height times.
  /// </summary>
  /// <param name="width">Rectangle width.</param>
  /// <param name="height">Rectangle height.</param>
  public abstract void Pass(int width, int height);
}

public class ZCurve : DefaultPixelOrder
{
  private int x;
  private int y;
  private int imageWidth;
  private int imageHeight;
  private int size;
  private int level;

  public ZCurve (PixelAction callback) : base(callback)
  {
  }

  public override void Pass (int width, int height)
  {
    imageHeight = height;
    imageWidth = width;
    size = 1 << (int)Math.Ceiling(Math.Log(Math.Max(width, height), 2));
    x = 0;
    y = 0;
    level = (int)Math.Log(size, 2);

    ZOrder(0, 0, size, level);
  }

  private bool isOutsideOfImage(int x, int y){
    if (x< 0 || x >= imageWidth || y< 0 || y >= imageHeight)
    {
      return true;
    }
    return false;
  }

private void ZOrder (int startX, int startY, int size, int level)
  {

    if (level == 0)
    {
      if(!isOutsideOfImage(startX + x, startY + y))
      {
        Callback(startX + x, startY + y);
      }
      return;
    }

    int halfSize = size / 2;

    // Move to the top-left quadrant
    ZOrder(startX, startY, halfSize, level - 1);

    // Move to the top-right quadrant
    ZOrder(startX + halfSize, startY, halfSize, level - 1);

    // Move to the bottom-left quadrant
    ZOrder(startX, startY + halfSize, halfSize, level - 1);

    // Move to the bottom-right quadrant
    ZOrder(startX + halfSize, startY + halfSize, halfSize, level - 1);
  }
}

public class HilbertCurve : DefaultPixelOrder
{
  private int order;
  private long total;

  public HilbertCurve (PixelAction callback) : base(callback)
  {
  }

  public override void Pass (int width, int height)
  {
    total = width * height;
    order = (int)Math.Ceiling(Math.Log(Math.Max(width, height), 2));

    Hilbert2D(0, 0, width, 0, 0, height);
  }

  private void Hilbert2D (int x, int y, int ax, int ay, int bx, int by)
  {
    int w = Math.Abs(ax + ay);
    int h = Math.Abs(bx + by);

    int dax = Math.Sign(ax);
    int day = Math.Sign(ay);
    int dbx = Math.Sign(bx);
    int dby = Math.Sign(by);

    if (h == 1)
    {
      // trivial row fill
      for (int i = 0; i < w; i++)
      {
        Callback(x, y);
        x += dax;
        y += day;
      }
      return;
    }

    if (w == 1)
    {
      // trivial column fill
      for (int i = 0; i < h; i++)
      {
        Callback(x, y);
        x += dbx;
        y += dby;
      }
      return;
    }

    int ax2 = ax / 2;
    int ay2 = ay / 2;
    int bx2 = bx / 2;
    int by2 = by / 2;

    int w2 = Math.Abs(ax2 + ay2);
    int h2 = Math.Abs(bx2 + by2);

    if (2 * w > 3 * h)
    {
      if ((w2 % 2 == 1) && (w > 2))
      {
        // prefer even steps
        ax2 += dax;
        ay2 += day;
      }

      // long case: split in two parts only
      Hilbert2D(x, y, ax2, ay2, bx, by);
      Hilbert2D(x + ax2, y + ay2, ax - ax2, ay - ay2, bx, by);
    }
    else
    {
      if ((h2 % 2 == 1) && (h > 2))
      {
        // prefer even steps
        bx2 += dbx;
        by2 += dby;
      }

      // standard case: one step up, one long horizontal, one step down
      Hilbert2D(x, y, bx2, by2, ax2, ay2);
      Hilbert2D(x + bx2, y + by2, ax, ay, bx - bx2, by - by2);
      Hilbert2D(x + (ax - dax) + (bx2 - dbx), y + (ay - day) + (by2 - dby),
                -bx2, -by2, -(ax - ax2), -(ay - ay2));
    }
  }
}


public class ZigZagScan : DefaultPixelOrder
{
  public ZigZagScan (PixelAction callback) : base(callback)
  { }

  /// <summary>
  /// Passes the given rectangle in a zig-zag order, calls the Callback() function for every pixel.
  /// The Callback() function should be called exactly width * height times.
  /// </summary>
  /// <param name="width">Rectangle width.</param>
  /// <param name="height">Rectangle height.</param>
  public override void Pass (int width, int height)
  {
    Debug.Assert(Callback != null);

    for (int y = 0; y < height; y++)
    {
      // Reverse direction on even rows
      int startX = (y % 2 == 0) ? 0 : width - 1;
      int endX = (y % 2 == 0) ? width : -1;
      int stepX = (y % 2 == 0) ? 1 : -1;

      for (int x = startX; x != endX; x += stepX)
      {
        Callback(x, y);
      }
    }
  }
}

/// <summary>
/// Pilot pixel ordering: ScanLine order.
/// You could create your own methods in a similar way.
/// </summary>
public class ScanLine : DefaultPixelOrder
{
  public ScanLine(PixelAction callback) : base(callback)
  {}

  /// <summary>
  /// Passes the given rectangle, calls the Callback() function for every pixel.
  /// The Callback() function should be called exactly width * height times.
  /// </summary>
  /// <param name="width">Rectangle width.</param>
  /// <param name="height">Rectangle height.</param>
  public override void Pass(int width, int height)
  {
    Debug.Assert(Callback != null);

    for (int y = 0; y < height; y++)
      for (int x = 0; x < width; x++)
        Callback(x, y);
  }
}


public class SpiralPixelOrder : DefaultPixelOrder
{
  public SpiralPixelOrder (PixelAction callback) : base(callback)
  { }

  public override void Pass (int width, int height)
  {
    Debug.Assert(Callback != null);

    int x = 0, y = 0;
    int dx = 0, dy = -1;
    int maxRight=width-1, maxBottom=height-1, maxLeft=0, maxTop=0;

    for (int i = 0; i < width*height; i++)
    {
      Callback(x, y);

      if (x == maxRight && y == maxTop)
      {
        // Change direction in the spiral
        int temp = dx;
        dx = -dy;
        dy = temp;
        if (x != width-1 && y != 0)
          maxLeft++;
      }
      else if (x == maxRight && y == maxBottom)
      {
        // Change direction in the spiral
        int temp = dx;
        dx = -dy;
        dy = temp;
        maxTop++;
      }
      else if (x == maxLeft && y == maxBottom)
      {
        // Change direction in the spiral
        int temp = dx;
        dx = -dy;
        dy = temp;
        maxRight--;
      }
      else if (x == maxLeft && y == maxTop)
      {
        // Change direction in the spiral
        int temp = dx;
        dx = -dy;
        dy = temp;
        if (!(x == 0 && y == 0))
         maxBottom--;
      }

      x += dx;
      y += dy;
    }
  }
}

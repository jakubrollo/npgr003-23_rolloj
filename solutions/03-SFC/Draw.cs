using System;
using System.Collections.Generic;
using System.Threading;

namespace _03_SFC
{
  class Draw
  {
    List<Coordinate> coordinates = new List<Coordinate>();

    public void DrawAll ()
    {
      Console.Clear();
      Console.ForegroundColor = ConsoleColor.Blue;
      foreach (var coordinate in coordinates)
      {
        DrawCoordinate(coordinate);
        Thread.Sleep(80); // Sleep for 1 second
      }
      Console.ResetColor();
      Console.Clear();
      Console.WriteLine("");
      Console.WriteLine("Drawing complete.");
    }

    void DrawCoordinate (Coordinate coordinate)
    {
      Console.SetCursorPosition(2*(coordinate.X), coordinate.Y);
      Console.Write("██"); // You can change this to a different character or use Console.Write(" ") for an empty space
    }

    public void AddCoordinate (int x, int y)
    {
      coordinates.Add(new Coordinate(x, y));
    }

    class Coordinate
    {
      public int X { get; }
      public int Y { get; }

      public Coordinate (int x, int y)
      {
        X = x;
        Y = y;
      }
    }
  }
}

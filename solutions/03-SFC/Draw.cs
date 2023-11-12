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

      foreach (var coordinate in coordinates)
      {
        DrawCoordinate(coordinate);
        Thread.Sleep(100); // Sleep for 1 second
      }

      Console.WriteLine("Drawing complete.");
    }

    void DrawCoordinate (Coordinate coordinate)
    {
      Console.SetCursorPosition(coordinate.X, coordinate.Y);
      Console.ForegroundColor = coordinate.Color;
      Console.Write("█"); // You can change this to a different character or use Console.Write(" ") for an empty space
      Console.ResetColor();
    }

    public void AddCoordinate (int x, int y, ConsoleColor color)
    {
      coordinates.Add(new Coordinate(x, y, color));
    }

    class Coordinate
    {
      public int X { get; }
      public int Y { get; }
      public ConsoleColor Color { get; }

      public Coordinate (int x, int y, ConsoleColor color)
      {
        X = x;
        Y = y;
        Color = color;
      }
    }
  }
}

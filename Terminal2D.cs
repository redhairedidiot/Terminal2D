using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal2D;

public class Vector2
{
    public Vector2(int x = 0, int y = 0)
    {
        X = x;
        Y = y;
    }

    public Vector2(Vector2 copying)
    {
        X = copying.X;
        Y = copying.Y;
    }

    public int X { get; init; }

    public int Y { get; init; }

    public static Vector2 Zero => new();

    public static double DistanceTo(Vector2 a, Vector2 b)
    {
        int distX = (b.X - a.X) * (b.X - a.X);
        int distY = (b.Y - a.Y) * (b.Y - a.Y);

        return Math.Sqrt(distX + distY);
    }
}

public static class Boundaries
{
    public enum BoundaryCorner
    {
        TopLeft,
        BottomLeft,
        TopRight,
        BottomRight,
    }

    public static Vector2 GetBoundingBox(Shape s)
    {
        if (s == null || s.Verticies.Count == 0)
        {
            return Vector2.Zero;
        }

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (Vector2 v in s.Verticies)
        {
            if (v.X < minX)
            {
                minX = v.X;
            }

            if (v.X > maxX)
            {
                maxX = v.X;
            }

            if (v.Y < minY)
            {
                minY = v.Y;
            }

            if (v.Y > maxY)
            {
                maxY = v.Y;
            }
        }

        int width = maxX - minX;
        int height = maxY - minY;

        return new Vector2(width, height);
    }

    public static Vector2 GetBoundaryPoint(Shape s, BoundaryCorner c)
    {
        if (s == null || s.Verticies.Count <= 0)
        {
            return Vector2.Zero;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;

        foreach (Vector2 v in s.Verticies)
        {
            if (v.X < minX)
            {
                minX = v.X;
            }

            if (v.Y < minY)
            {
                minY = v.Y;
            }
        }

        Vector2 dim = GetBoundingBox(s);
        int width = dim.X;
        int height = dim.Y;

        return c switch
        {
            BoundaryCorner.TopLeft => new Vector2(minX, minY),
            BoundaryCorner.TopRight => new Vector2(minX + width, minY),
            BoundaryCorner.BottomLeft => new Vector2(minX, minY + height),
            BoundaryCorner.BottomRight => new Vector2(minX + width, minY + height),
            _ => Vector2.Zero,
        };
    }

    public static bool IsInsideBoundary(Shape s, Vector2 p)
    {
        Vector2 topLeft = GetBoundaryPoint(s, BoundaryCorner.TopLeft);
        Vector2 bounding = GetBoundingBox(s);

        return p.X >= topLeft.X
            && p.X <= topLeft.X + bounding.X
            && p.Y >= topLeft.Y
            && p.Y <= topLeft.Y + bounding.Y;
    }

    public static Vector2 GetCenterPoint(Shape s)
    {
        if (s == null || s.Verticies.Count <= 0)
        {
            return Vector2.Zero;
        }

        Vector2 topLeft = GetBoundaryPoint(s, BoundaryCorner.TopLeft);
        Vector2 dim = GetBoundingBox(s);

        int centX = topLeft.X + (dim.X / 2);
        int centY = topLeft.Y + (dim.Y / 2);

        return new Vector2(centX, centY);
    }
}

public class Shape
{
    public Shape(Vector2 a, Vector2 b, Vector2 c, params Vector2[] extras)
    {
        Verticies = new() { a, b, c };

        foreach (Vector2 extra in extras)
        {
            Verticies.Add(extra);
        }
    }

    public List<Vector2> Verticies { get; init; }

    public static Shape Square => new(Vector2.Zero, new Vector2(5, 0), new Vector2(5, 5), new Vector2(0, 5));

    public static Shape Transform(Shape s, Vector2 amt)
    {
        if (s == null || s.Verticies.Count <= 0)
        {
            throw new ArgumentException("Invalid s for Shape.Transform(Shape s, Vector2 amt)", nameof(s));
        }

        List<Vector2> newV = new();

        foreach (Vector2 v in s.Verticies)
        {
            newV.Add(new Vector2(v.X + amt.X, v.Y + amt.Y));
        }

        return new Shape(newV[0], newV[1], newV[2], newV.Skip(3).ToArray());
    }

    public static Shape Rotate(Shape s, Vector2 o, double deg)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s), "s is null on Shape.Rotate(Shape s, Vector2 o, double deg)");
        }

        Vector2 topLeft = Boundaries.GetBoundaryPoint(s, Boundaries.BoundaryCorner.TopLeft);

        int pivotX = topLeft.X + o.X;
        int pivotY = topLeft.Y + o.Y;

        double rad = deg * (Math.PI / 180.0);
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        List<Vector2> rotated = new();

        foreach (Vector2 v in s.Verticies)
        {
            double tX = v.X - pivotX;
            double tY = v.Y - pivotY;

            double newX = (tX * cos) - (tY * sin);
            double newY = (tX * sin) + (tY * cos);

            int finalX = (int)Math.Round(newX + pivotX);
            int finalY = (int)Math.Round(newY + pivotY);

            rotated.Add(new Vector2(finalX, finalY));
        }

        return new Shape(rotated[0], rotated[1], rotated[2], rotated.Skip(3).ToArray());
    }

    public static Shape Scale(Shape s, Vector2 factor)
    {
        if (s == null || s.Verticies.Count <= 0)
        {
            throw new ArgumentException("s is invalid in Shape.Scale(Shape s, Vector2 factor)", nameof(s));
        }

        Vector2 center = Boundaries.GetCenterPoint(s);

        List<Vector2> scaled = new();

        foreach (Vector2 v in s.Verticies)
        {
            double localX = v.X - center.X;
            double localY = v.Y - center.Y;

            int newX = (int)Math.Round(center.X + (localX * factor.X));
            int newY = (int)Math.Round(center.Y + (localY * factor.Y));

            scaled.Add(new Vector2(newX, newY));
        }

        return new Shape(scaled[0], scaled[1], scaled[2], scaled.Skip(3).ToArray());
    }
}

public static class Screen
{
    private static List<List<bool>> _pixels = new();

    public static int Width { get; set; }

    public static int Height { get; set; }

    public static int CenterX => Width / 2;

    public static int CenterY => Height / 2;

    public static void Create(int resX = 0, int resY = 0)
    {
        Width = resX == 0 ? Width : resX;
        Height = resY == 0 ? Height : resY;

        _pixels = new List<List<bool>>();

        for (int y = 0; y < Height; y++)
        {
            List<bool> build = new();

            for (int x = 0; x < Width; x++)
            {
                build.Add(false);
            }

            _pixels.Add(build);
        }
    }

    public static void Reset()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _pixels[y][x] = false;
            }
        }
    }

    public static void DrawPixel(Vector2 p)
    {
        if (p == null)
        {
            throw new ArgumentNullException(nameof(p), "p was null on Screen.DrawPixel(Vector2 p)");
        }

        if (p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height)
        {
            _pixels[p.Y][p.X] = true;
        }
    }

    public static void DrawLine(Vector2 a, Vector2 b)
    {
        int ax = a.X;
        int ay = a.Y;
        int bx = b.X;
        int by = b.Y;

        int dx = Math.Abs(bx - ax);
        int dy = Math.Abs(by - ay);

        int sx = ax < bx ? 1 : -1;
        int sy = ay < by ? 1 : -1;

        int err = (dx > dy ? dx : -dy) / 2;

        while (true)
        {
            if (ay >= 0 && ay < Height && ax >= 0 && ax < Width)
            {
                _pixels[ay][ax] = true;
            }

            if (ax == bx && ay == by)
            {
                break;
            }

            int e2 = err;

            if (e2 > -dx)
            {
                err -= dy;
                ax += sx;
            }

            if (e2 < dy)
            {
                err += dx;
                ay += sy;
            }
        }
    }

    public static void DrawShape(Shape shape, bool fill = false)
    {
        if (shape == null || shape.Verticies.Count < 3)
        {
            throw new InvalidTerminal2DShapeException("Invalid Terminal2D shape. Either null or has less than 3 verticies.");
        }

        int count = shape.Verticies.Count;

        if (fill)
        {
            int minY = Height;
            int maxY = -1;

            foreach (Vector2 v in shape.Verticies)
            {
                if (v.Y < minY)
                {
                    minY = v.Y;
                }

                if (v.Y > maxY)
                {
                    maxY = v.Y;
                }
            }

            minY = Math.Max(0, minY);
            maxY = Math.Min(Height - 1, maxY);

            for (int y = minY; y <= maxY; y++)
            {
                List<int> nodeX = new();

                for (int i = 0; i < count; i++)
                {
                    Vector2 a = shape.Verticies[i];
                    Vector2 b = shape.Verticies[(i + 1) % count];

                    if (a.Y == b.Y)
                    {
                        continue;
                    }

                    if ((a.Y < y && b.Y >= y) || (b.Y < y && a.Y >= y))
                    {
                        int intX = a.X + ((y - a.Y) * (b.X - a.X) / (b.Y - a.Y));
                        nodeX.Add(intX);
                    }
                }

                nodeX.Sort();

                for (int i = 0; i + 1 < nodeX.Count; i += 2)
                {
                    int startX = Math.Max(0, nodeX[i]);
                    int endX = Math.Min(Width - 1, nodeX[i + 1]);

                    for (int x = startX; x <= endX; x++)
                    {
                        _pixels[y][x] = true;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 currPoint = shape.Verticies[i];
                Vector2 nextPoint = shape.Verticies[(i + 1) % count];

                DrawLine(currPoint, nextPoint);
            }
        }
    }

    public static void Render(string fill = "██", bool borders = true)
    {
        if (Height == 0)
        {
            throw new InvalidTerminal2DPixelException("Invalid pixels provided in Terminal2D, create them using Terminal2D.Screen.Create(int resX, int resY)");
        }

        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        Console.Write("\u001b[3J\u001b[H\u001b[2J");
        Console.Clear();

        if (borders)
        {
            StringBuilder border = new();

            for (int x = 0; x < Width; x++)
            {
                border.Append("--");
            }

            Console.WriteLine(border.ToString());
        }

        foreach (List<bool> row in _pixels)
        {
            StringBuilder build = new();

            for (int x = 0; x < Width; x++)
            {
                bool pixel = row[x];

                build.Append(pixel ? fill : "  ");

                if (borders && x >= Width - 1)
                {
                    build.Append(" |");
                }
            }

            Console.WriteLine(build.ToString());
        }
    }

    public class InvalidTerminal2DPixelException : Exception
    {
        public InvalidTerminal2DPixelException(string message)
            : base(message)
        {
        }
    }

    public class InvalidTerminal2DShapeException : Exception
    {
        public InvalidTerminal2DShapeException(string message)
            : base(message)
        {
        }
    }
}

public static class TaskManager
{
    private static readonly Dictionary<int, Action<double>> _queue = new();
    private static readonly List<Dictionary<string, object>> _onceQueue = new();

    private static bool _running;

    public static void Schedule(int id, Action<double> act)
    {
        _queue[id] = act;
    }

    public static void QueueOnce(Action act, int wait)
    {
        if (wait <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wait), "Wait must be over 0 for the QueueOnce(Action act, int wait) method");
        }

        _onceQueue.Add(new Dictionary<string, object>()
        {
            ["action"] = act,
            ["timeLeft"] = wait,
        });
    }

    public static void Cancel(int id)
    {
        _queue.Remove(id);
    }

    public static void Begin(int fps = 30)
    {
        if (_running)
        {
            return;
        }

        _running = true;

        int intMs = 1_000 / fps;

        Task.Run(async () =>
        {
            System.Diagnostics.Stopwatch deltaTimer = new();
            deltaTimer.Start();

            while (_running)
            {
                double elapsedSecs = deltaTimer.Elapsed.TotalSeconds;
                deltaTimer.Restart();

                int[] activeKeys = new int[_queue.Count];

                double deltaTime = Math.Min(elapsedSecs, 0.1);

                _queue.Keys.CopyTo(activeKeys, 0);

                foreach (int key in activeKeys)
                {
                    if (_queue.TryGetValue(key, out Action<double>? act))
                    {
                        act.Invoke(deltaTime);
                    }
                }

                for (int i = _onceQueue.Count - 1; i >= 0; i--)
                {
                    Dictionary<string, object> queued = _onceQueue[i];

                    var act = (Action)queued["action"];
                    int timeLeft = (int)queued["timeLeft"];

                    timeLeft -= 1;

                    if (timeLeft <= 0)
                    {
                        act();
                        _onceQueue.RemoveAt(i);
                    }
                    else
                    {
                        queued["timeLeft"] = timeLeft;
                        _onceQueue[i] = queued;
                    }
                }

                await Task.Delay(intMs);
            }
        });
    }

    public static void Halt(bool clear = false)
    {
        _running = false;

        if (clear)
        {
            _queue.Clear();
        }
    }

    public static Task EndScript()
    {
        return new TaskCompletionSource().Task;
    }
}

public static class Input
{
    private static readonly List<InputListener> _registry = new();

    public static void Register(InputListener l)
    {
        if (l == null)
        {
            throw new ArgumentNullException(nameof(l), "InputListener is invalid, null.");
        }

        _registry.Add(l);
    }

    public static void Update()
    {
        foreach (InputListener l in _registry)
        {
            l.IsPressed = false;
        }

        while (Console.KeyAvailable)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            string currTap = keyInfo.Key.ToString();

            foreach (InputListener l in _registry)
            {
                if (l.TargetKey == currTap)
                {
                    l.IsPressed = true;
                }
            }
        }

        foreach (InputListener l in _registry)
        {
            if (l.IsPressed)
            {
                l.FramesHeld++;
            }
            else
            {
                l.FramesHeld = 0;
            }
        }
    }

    public class InputListener
    {
        public InputListener(string targetKey)
        {
            TargetKey = targetKey;
            IsPressed = false;
            FramesHeld = 0;
        }

        public string TargetKey { get; init; }

        public bool IsPressed { get; set; }

        public int FramesHeld { get; set; }
    }
}

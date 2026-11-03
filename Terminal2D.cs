using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Terminal2D
{
    public class Vector2
    {   
        public int X { get; init; }
        public int Y { get; init; }

        public static double DistanceTo(Vector2 x, Vector2 y)
        {
            int distX = (y.X - x.X) * (y.X - x.X);
            int distY = (y.Y - x.Y) * (y.Y - x.Y);

            return Math.Sqrt(distX + distY);
        }

        public Vector2(int x = 0, int y = 0)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Zero => new Vector2();
    }

    public static class Boundaries
    {
        public enum BoundaryCorner
        {
            TopLeft,
            BottomLeft,
            TopRight,
            BottomRight
        }

        public static Vector2 GetBoundingBox(Shape s)
        {
            if (s == null || s.Verticies.Count == 0) return Vector2.Zero;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (Vector2 v in s.Verticies)
            {
                if (v.X < minX) minX = v.X;
                if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Y > maxY) maxY = v.Y;
            }

            int width = maxX - minX;
            int height = maxY - minY;

            return new Vector2(width, height);
        }

        public static Vector2 GetBoundaryPoint(Shape s, BoundaryCorner c)
        {
            if (s == null || s.Verticies.Count <= 0) return Vector2.Zero;

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            foreach (Vector2 v in s.Verticies)
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
            }

            // Updated to use the correct 'GetBoundingBoxes' name from your new class
            Vector2 dim = GetBoundingBox(s); 
            int width = dim.X;
            int height = dim.Y;

            if (c == BoundaryCorner.TopLeft) return new Vector2(minX, minY);
            else if (c == BoundaryCorner.TopRight) return new Vector2(minX + width, minY);
            else if (c == BoundaryCorner.BottomLeft) return new Vector2(minX, minY + height);
            else if (c == BoundaryCorner.BottomRight) return new Vector2(minX + width, minY + height);
            else return Vector2.Zero;
        }

        public static bool IsInsideBoundary(Shape s, Vector2 p)
        {
            Vector2 topLeft = GetBoundaryPoint(s, BoundaryCorner.TopLeft);
            Vector2 bounding = GetBoundingBox(s);

            if (p.X >= topLeft.X && p.X <= topLeft.X + bounding.X && p.Y >= topLeft.Y && p.Y <= topLeft.Y + bounding.Y) return true;
            else return false;
        }

        public static Vector2 GetCenterPoint(Shape s)
        {
            if (s == null || s.Verticies.Count <= 0) return Vector2.Zero;

            Vector2 topLeft = GetBoundaryPoint(s, BoundaryCorner.TopLeft);
            Vector2 dim = GetBoundingBox(s);

            int centX = topLeft.X + (dim.X / 2);
            int centY = topLeft.Y + (dim.Y / 2);

            return new Vector2(centX, centY);
        }
    }

    public class Shape
    {
        public List<Vector2> Verticies { get; init; }

        public Shape(Vector2 x, Vector2 y, Vector2 z, params Vector2[] extras)
        {
            Verticies = new() {x, y, z};

            foreach (Vector2 extra in extras) Verticies.Add(extra);
        }

        public static Shape Transform(Shape s, Vector2 amt)
        {
            if (s == null || s.Verticies.Count <= 0) throw new ArgumentException("Invalid s for Shape.Transform(Shape s, Vector2 amt)");

            List<Vector2> newV = new();

            foreach (Vector2 v in s.Verticies) newV.Add(new Vector2(v.X + amt.X, v.Y + amt.Y));

            return new Shape(newV[0], newV[1], newV[2], newV.Skip(3).ToArray());
        }

        public static Shape Square => new Shape(Vector2.Zero, new Vector2(5, 0), new Vector2(5, 5), new Vector2(0, 5));

        public static Shape Rotate(Shape s, Vector2 o, double deg)
        {
            if (s == null) throw new ArgumentNullException("s is null on Shape.Rotate(Shape s, Vector2 o, double deg)");

            Vector2 topLeftCorner = Boundaries.GetBoundaryPoint(s, Boundaries.BoundaryCorner.TopLeft);
            int centX = topLeftCorner.X;
            int centY = topLeftCorner.Y;

            int pivotX = centX + o.X;
            int pivotY = centY + o.Y;

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
            if (s == null || s.Verticies.Count <= 0) throw new ArgumentException("s is invalid in Shape.Scale(Shape s, Vector2 factor)");

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
        static List<List<bool>> pixels = new();

        public class InvalidTerminal2DPixelException : Exception
        {
            public InvalidTerminal2DPixelException(string message) : base(message) { }
        }

        public static void Create(int resX = 0, int resY = 0)
        {
            Width = resX == 0 ? Width : resX; Height = resY == 0 ? Height : resY;

            pixels = new List<List<bool>>();

            for (int y = 0; y < Height; y++)
            {
                List<bool> build = new();

                for (int x = 0; x < Width; x++)
                {
                    build.Add(false);
                }

                pixels.Add(build);
            }
        }

        public static void Reset()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    pixels[y][x] = false;
                }
            }
        }

        public static void DrawPixel(Vector2 p)
        {
            if (p == null) throw new ArgumentNullException("p was null on Screen.DrawPixel(Vector2 p)");

            if (p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height) pixels[p.Y][p.X] = true;
        }

        public static void DrawLine(Vector2 x, Vector2 y)
        {
            int xx = x.X;
            int xy = x.Y;
            int yx = y.X;
            int yy = y.Y;

            int dx = Math.Abs(yx - xx);
            int dy = Math.Abs(yy - xy);

            int sx = xx < yx ? 1 : -1;
            int sy = xy < yy ? 1 : -1;

            int err = (dx > dy ? dx : -dy) / 2;
            int e2;

            while (true)
            {
                if (xy >= 0 && xy < Height && xx >= 0 && xx < Width) pixels[xy][xx] = true;

                if (xx == yx && xy == yy) break;

                e2 = err;

                if (e2 > -dx)
                {
                    err -= dy;
                    xx += sx;
                }

                if (e2 < dy)
                {
                    err += dx;
                    xy += sy;
                }
            }
        }

        public class InvalidTerminal2DShapeException : Exception
        {
            public InvalidTerminal2DShapeException(string message) : base(message) {}
        }

        public static void DrawShape(Shape shape, bool fill = false)
        {
            if (shape == null || shape.Verticies.Count < 3) throw new InvalidTerminal2DShapeException("Invalid Terminal2D shape. Either null or has less than 3 verticies.");

            int count = shape.Verticies.Count;

            if (fill)
            {
                int minY = Height;
                int maxY = -1;

                foreach (Vector2 v in shape.Verticies)
                {
                    if (v.Y < minY) minY = v.Y;
                    if (v.Y > maxY) maxY = v.Y;
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

                        if (a.Y == b.Y) continue;

                        if ((a.Y < y && b.Y >= y) || (b.Y < y && a.Y >= y))
                        {
                            int intX = a.X + (y - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                            nodeX.Add(intX);
                        }
                    }

                    nodeX.Sort();

                    for (int i = 0; i <= nodeX.Count; i += 2)
                    {
                        if (i + 1 >= nodeX.Count) break;

                        int startX = Math.Max(0, nodeX[i]);
                        int endX = Math.Min(Width - 1, nodeX[i + 1]);

                        for (int x = startX; x <= endX; x++) pixels[y][x] = true;
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
            if (Height == 0) throw new InvalidTerminal2DPixelException("Invalid pixels provided in Terminal2D, create them using Terminal2D.Screen.Create(int resX, int resY)");

            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.Write("\x1b[3J\x1b[H\x1b[2J");
            Console.Clear();

            if (borders)
            {
                StringBuilder build = new();

                for (int x = 0; x <= Width - 1; x++)
                {
                    build.Append("--");
                }

                Console.WriteLine(build.ToString());
            }

            foreach (List<bool> row in pixels)
            {
                StringBuilder build = new();

                for (int x = 0; x <= Width - 1; x++)
                {
                    bool pixel = row[x];

                    build.Append(pixel ? fill : "  ");
                    if (borders && x >= Width - 1) build.Append(" |");
                }

                Console.WriteLine(build.ToString());
            }
        }

        public static int Width = 0;
        public static int Height = 0;

        public static int CenterX => Width / 2;
        public static int CenterY => Height / 2;
    }

    public static class TaskManager
    {
        static readonly Dictionary<int, Action<double>> queue = new();
        static readonly List<Dictionary<string, object>> onceQueue = new();

        static bool running = false;

        public static void Schedule(int id, Action<double> act) {queue[id] = act;}

        public static void QueueOnce(Action act, int wait) 
        {
            if (wait > 0)
                onceQueue.Add(new Dictionary<string, object>()
                    {
                        ["action"] = act,
                        ["timeLeft"] = wait
                    });
            else
                throw new ArgumentOutOfRangeException("Wait must be over 0 for the QueueOnce(Action act, int wait) method");
        }

        public static void Cancel(int id) {queue.Remove(id);}

        public static void Begin(int fps = 30)
        {
            if (running) return;

            running = true;

            int intMs = 1_000 / fps;

            Task.Run(async () =>
            {
                System.Diagnostics.Stopwatch deltaTimer = new();
                deltaTimer.Start();

                while (running)
                {
                    double elapsedSecs = deltaTimer.Elapsed.TotalSeconds;
                    deltaTimer.Restart();

                    int[] activeKeys = new int[queue.Count];

                    double deltaTime = Math.Min(elapsedSecs, 0.1);

                    queue.Keys.CopyTo(activeKeys, 0);

                    foreach (int key in activeKeys)
                    {
                        if (queue.TryGetValue(key, out Action<double>? act)) act!.Invoke(deltaTime);
                    }

                    for (int i = onceQueue.Count - 1; i >= 0; i--)
                    {
                        Dictionary<string, object> queued = onceQueue[i];

                        Action act = (Action)queued["action"];
                        int timeLeft = (int)queued["timeLeft"];

                        timeLeft -= 1;

                        if (timeLeft <= 0)
                        {
                            act();
                            onceQueue.RemoveAt(i); 
                        }
                        else
                        {
                            queued["timeLeft"] = timeLeft;
                            onceQueue[i] = queued;
                        }
                    }

                    await Task.Delay(intMs);
                }
            }
            );
        }

        public static void Halt(bool clear = false)
        {
            running = false;
            if (clear) queue.Clear();
        }

        public static Task EndScript()
        {
            return new TaskCompletionSource().Task;
        }
    }

    public static class Input
    {
        public class InputListener
        {
            public string TargetKey { get; init; }
            public bool IsPressed { get; set; }

            public int FramesHeld { get; set; }

            public InputListener(string targetKey)
            {
                TargetKey = targetKey;
                IsPressed = false;
                FramesHeld = 0;
            }
        }

        static List<InputListener> registry = new();

        public static void Register(InputListener l)
        {
            if (l == null) throw new ArgumentNullException("InputListener is invalid, null.");

            registry.Add(l);
        }

        public static void Update()
        {
            foreach (InputListener l in registry) l.IsPressed = false;

            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                string currTap = keyInfo.Key.ToString();

                foreach (InputListener l in registry)
                {
                    if (l.TargetKey == currTap) l.IsPressed = true;
                }
            }

            foreach (InputListener l in registry)
            {
                if (l.IsPressed) l.FramesHeld++;
                else l.FramesHeld = 0;
            }
        }
    }
}
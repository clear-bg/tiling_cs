using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

class AxisSplitter
{
    class Point
    {
        public float x, y, z;
        public byte r, g, b;

        public Point(float x, float y, float z, byte r, byte g, byte b)
        {
            this.x = x; this.y = y; this.z = z;
            this.r = r; this.g = g; this.b = b;
        }
    }

    static void Main(string[] args)
    {
        // 実行ファイル基準のルートパス（tiling_cs）
        string baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));

        // 入出力ディレクトリ
        string inputPath = Path.Combine(baseDir, "Original_Ply", "loot_vox10_0000.ply");
        string outputDir = Path.Combine(baseDir, "tiled_Ply");
        Directory.CreateDirectory(outputDir);

        var points = new List<Point>();
        int vertexCount = 0;
        string format = "ascii";
        int headerLength = 0;

        using (var fs = new FileStream(inputPath, FileMode.Open))
        using (var reader = new StreamReader(fs, Encoding.ASCII, false, 1024, true))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                headerLength += Encoding.ASCII.GetByteCount(line + "\n");
                if (line.StartsWith("format")) format = line.Split(' ')[1];
                if (line.StartsWith("element vertex")) vertexCount = int.Parse(line.Split(' ')[2]);
                if (line.StartsWith("end_header")) break;
            }

            if (format == "ascii")
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    var tokens = reader.ReadLine().Split(' ');
                    float x = float.Parse(tokens[0], CultureInfo.InvariantCulture);
                    float y = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                    float z = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                    byte r = byte.Parse(tokens[3]);
                    byte g = byte.Parse(tokens[4]);
                    byte b = byte.Parse(tokens[5]);
                    points.Add(new Point(x, y, z, r, g, b));
                }
            }
            else if (format == "binary_little_endian")
            {
                reader.Dispose();
                fs.Seek(headerLength, SeekOrigin.Begin);
                using (var bin = new BinaryReader(fs, Encoding.ASCII, true))
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        float x = bin.ReadSingle();
                        float y = bin.ReadSingle();
                        float z = bin.ReadSingle();
                        byte r = bin.ReadByte();
                        byte g = bin.ReadByte();
                        byte b = bin.ReadByte();
                        points.Add(new Point(x, y, z, r, g, b));
                    }
                }
            }
            else
            {
                Console.WriteLine("対応していない形式です: " + format);
                return;
            }
        }

        // 各軸ごとの分割
        (float minX, float maxX) = GetMinMax(points, p => p.x);
        (float minY, float maxY) = GetMinMax(points, p => p.y);
        (float minZ, float maxZ) = GetMinMax(points, p => p.z);

        SplitAndWrite(points, p => p.x, minX, maxX, Path.Combine(outputDir, "loot_x_tile_"));
        SplitAndWrite(points, p => p.y, minY, maxY, Path.Combine(outputDir, "loot_y_tile_"));
        SplitAndWrite(points, p => p.z, minZ, maxZ, Path.Combine(outputDir, "loot_z_tile_"));

        Console.WriteLine("XYZ軸3分割が完了しました！");
    }

    static (float, float) GetMinMax(List<Point> points, Func<Point, float> selector)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (var p in points)
        {
            float v = selector(p);
            if (v < min) min = v;
            if (v > max) max = v;
        }
        return (min, max);
    }

    static void SplitAndWrite(List<Point> points, Func<Point, float> selector, float min, float max, string prefix)
    {
        float step = (max - min) / 3f;
        var buckets = new List<Point>[3];
        for (int i = 0; i < 3; i++) buckets[i] = new List<Point>();

        foreach (var p in points)
        {
            float value = selector(p);
            int index = Math.Min((int)((value - min) / step), 2);
            buckets[index].Add(p);
        }

        for (int i = 0; i < 3; i++)
        {
            string filename = prefix + i + ".ply";
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine("ply");
                writer.WriteLine("format ascii 1.0");
                writer.WriteLine($"element vertex {buckets[i].Count}");
                writer.WriteLine("property float x");
                writer.WriteLine("property float y");
                writer.WriteLine("property float z");
                writer.WriteLine("property uchar red");
                writer.WriteLine("property uchar green");
                writer.WriteLine("property uchar blue");
                writer.WriteLine("end_header");

                foreach (var p in buckets[i])
                {
                    writer.WriteLine($"{p.x.ToString(CultureInfo.InvariantCulture)} {p.y.ToString(CultureInfo.InvariantCulture)} {p.z.ToString(CultureInfo.InvariantCulture)} {p.r} {p.g} {p.b}");
                }
            }
            Console.WriteLine($"{filename} を出力しました（{buckets[i].Count}点）");
        }
    }
}

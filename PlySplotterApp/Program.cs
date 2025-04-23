using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

class PLYSplitter
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
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string inputPath = Path.Combine(baseDir, @"..\..\..\..\Original_Ply\loot_vox10_0000.ply");
        string outputDir = Path.Combine(baseDir, @"..\..\..\..\tiled_Ply");
        Directory.CreateDirectory(outputDir);
        string outputPrefix = Path.Combine(outputDir, "loot_tile_");

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

                if (line.StartsWith("format"))
                    format = line.Split(' ')[1];
                if (line.StartsWith("element vertex"))
                    vertexCount = int.Parse(line.Split(' ')[2]);
                if (line.StartsWith("end_header"))
                    break;
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

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var p in points)
        {
            if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            if (p.z < minZ) minZ = p.z; if (p.z > maxZ) maxZ = p.z;
        }

        float stepX = (maxX - minX) / 2f;
        float stepY = (maxY - minY) / 3f;
        float stepZ = (maxZ - minZ) / 2f;

        var tiles = new List<Point>[12];
        for (int i = 0; i < 12; i++) tiles[i] = new List<Point>();

        foreach (var p in points)
        {
            int ix = (p.x == maxX) ? 1 : (int)((p.x - minX) / stepX);
            int iy = (p.y == maxY) ? 2 : (int)((p.y - minY) / stepY);
            int iz = (p.z == maxZ) ? 1 : (int)((p.z - minZ) / stepZ);

            int index = iz * 6 + iy * 2 + ix;
            tiles[index].Add(p);
        }

        for (int i = 0; i < 12; i++)
        {
            string outFile = $"{outputPrefix}{i:D2}.ply";
            using (var writer = new StreamWriter(outFile))
            {
                writer.WriteLine("ply");
                writer.WriteLine("format ascii 1.0");
                writer.WriteLine($"element vertex {tiles[i].Count}");
                writer.WriteLine("property float x");
                writer.WriteLine("property float y");
                writer.WriteLine("property float z");
                writer.WriteLine("property uchar red");
                writer.WriteLine("property uchar green");
                writer.WriteLine("property uchar blue");
                writer.WriteLine("end_header");

                foreach (var p in tiles[i])
                {
                    writer.WriteLine($"{p.x.ToString(CultureInfo.InvariantCulture)} {p.y.ToString(CultureInfo.InvariantCulture)} {p.z.ToString(CultureInfo.InvariantCulture)} {p.r} {p.g} {p.b}");
                }
            }
            Console.WriteLine($"Tile {i} -> {outFile} に保存しました（{tiles[i].Count}点）");
        }

        Console.WriteLine("12分割（Y3, X2, Z2）が完了しました！");
    }
}

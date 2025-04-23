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
        string inputPath = "Loot.ply"; // 入力ファイル
        string outputPrefix = "Loot_tile_"; // 出力ファイルの接頭辞

        var points = new List<Point>();
        int vertexCount = 0;
        string format = "ascii";
        int headerLength = 0;

        // --- ヘッダ読み込み ---
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

            // --- 頂点情報の読み込み ---
            if (format == "ascii")
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    var dataLine = reader.ReadLine();
                    var tokens = dataLine.Split(' ');
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
                reader.Dispose(); // バイナリに切り替え

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

        // --- 範囲計算 ---
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
        float stepY = (maxY - minY) / 2f;
        float stepZ = (maxZ - minZ) / 3f;

        // --- タイル分け ---
        var tiles = new List<Point>[12];
        for (int i = 0; i < 12; i++) tiles[i] = new List<Point>();

        foreach (var p in points)
        {
            int ix = Math.Min((int)((p.x - minX) / stepX), 1);
            int iy = Math.Min((int)((p.y - minY) / stepY), 1);
            int iz = Math.Min((int)((p.z - minZ) / stepZ), 2);
            int index = iz * 4 + iy * 2 + ix;
            tiles[index].Add(p);
        }

        // --- 出力（ASCII形式） ---
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
            Console.WriteLine($"Tile {i} -> {outFile} に保存しました");
        }

        Console.WriteLine("すべての処理が完了しました！");
    }
}

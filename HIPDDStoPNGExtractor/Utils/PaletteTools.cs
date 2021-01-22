using System;
using System.Drawing;
using System.IO;

namespace HIPDDStoPNGExtractor.Utils
{
    public static class PaletteTools
    {
        public static Color[] ReadACTPalette(byte[] bytes, int colorRange)
        {
            try
            {
                using (var reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    var colors = new Color[colorRange];
                    for (var i = 0; i < colorRange; i++)
                    {
                        var red = reader.ReadByte();
                        var green = reader.ReadByte();
                        var blue = reader.ReadByte();
                        colors[i] = Color.FromArgb(0xFF, red, green, blue);
                    }

                    reader.Close();
                    return colors;
                }
            }
            catch
            {
                return null;
            }
        }

        public static Color[] ReadPALPalette(byte[] bytes, int colorRange)
        {
            try
            {
                using (var reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    reader.BaseStream.Seek(22, SeekOrigin.Begin);
                    int palColorRange = reader.ReadInt16();
                    if ((uint) palColorRange > (uint) colorRange)
                        palColorRange = colorRange;
                    var colors = new Color[colorRange];
                    for (var _i = 0; _i < palColorRange; _i++)
                    {
                        var red = reader.ReadByte();
                        var green = reader.ReadByte();
                        var blue = reader.ReadByte();
                        var alpha = reader.ReadByte();
                        colors[colors.Length - 1 - _i] = Color.FromArgb(alpha, red, green, blue);
                    }

                    return colors;
                }
            }
            catch
            {
                return null;
            }
        }

        public static Color[] ReadACOPalette(byte[] bytes, int colorRange)
        {
            try
            {
                using (var reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    reader.BaseStream.Seek(2, SeekOrigin.Current);
                    var colorrangebytes = reader.ReadBytes(2);
                    Array.Reverse(colorrangebytes);
                    var colorrange = (int) BitConverter.ToInt16(colorrangebytes, 0);
                    reader.BaseStream.Seek(2, SeekOrigin.Current);
                    if (colorrange > colorRange)
                        colorrange = colorRange;
                    var colors = new Color[colorrange];
                    for (var i = 0; i < colorrange; i++)
                    {
                        var red = reader.ReadByte();
                        reader.ReadByte();
                        var green = reader.ReadByte();
                        reader.ReadByte();
                        var blue = reader.ReadByte();
                        reader.ReadBytes(5);
                        colors[i] = Color.FromArgb(0xFF, red, green, blue);
                    }

                    reader.Close();
                    return colors;
                }
            }
            catch
            {
                return null;
            }
        }

        public static Color[] ReadASEPalette(byte[] bytes, int colorRange)
        {
            try
            {
                using (var reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    reader.BaseStream.Seek(8, SeekOrigin.Current);
                    var totalblocksbytes = reader.ReadBytes(4);
                    Array.Reverse(totalblocksbytes);
                    var totalblocks = BitConverter.ToInt32(totalblocksbytes, 0);
                    if (totalblocks > colorRange)
                        totalblocks = colorRange;
                    var colors = new Color[totalblocks];
                    for (var i = 0; i < totalblocks; i++)
                    {
                        reader.BaseStream.Seek(2, SeekOrigin.Current);
                        var blocklengthbytes = reader.ReadBytes(4);
                        Array.Reverse(blocklengthbytes);
                        var blocklength = BitConverter.ToInt32(blocklengthbytes, 0);
                        if (blocklength == 0)
                            break;
                        var blockstringlengthbytes = reader.ReadBytes(2);
                        Array.Reverse(blockstringlengthbytes);
                        var blockstringlength = BitConverter.ToInt16(blockstringlengthbytes, 0) * 2;
                        reader.ReadBytes(blockstringlength);
                        if (blocklength - blockstringlength == 20)
                        {
                            reader.BaseStream.Seek(6, SeekOrigin.Current);
                            var red = reader.ReadByte();
                            reader.BaseStream.Seek(3, SeekOrigin.Current);
                            var green = reader.ReadByte();
                            reader.BaseStream.Seek(3, SeekOrigin.Current);
                            var blue = reader.ReadByte();
                            reader.BaseStream.Seek(3, SeekOrigin.Current);
                            colors[i] = Color.FromArgb(0xFF, red, green, blue);
                        }
                        else
                        {
                            --i;
                        }
                    }

                    reader.Close();
                    return colors;
                }
            }
            catch
            {
                return null;
            }
        }

        public static byte[] CreateACTByteArray(Color[] colors)
        {
            var length = colors.Length * 3;
            var bytes = new byte[length];
            using (var binaryWriter = new BinaryWriter(new MemoryStream(bytes)))
            {
                for (var i = 0; i < colors.Length; i++)
                {
                    binaryWriter.Write(colors[i].R);
                    binaryWriter.Write(colors[i].G);
                    binaryWriter.Write(colors[i].B);
                }

                ;
            }

            return bytes;
        }
    }
}
namespace Dealoware.Api.Tests;

/// <summary>
/// Reads a QR Code symbol back from its modules, the way a scanner does once
/// it has found the grid: format information, mask, codewords, blocks, error
/// correction check, and the byte-mode text.
/// <para>
/// It is written from the standard (ISO/IEC 18004) and shares nothing with
/// the encoder under test: its own tables (the published totals and data
/// codeword counts, not the encoder's per-block numbers), the alignment
/// centres worked out by formula where the encoder lists them, field
/// arithmetic by logarithm tables where the encoder multiplies bit by bit,
/// and the error correction checked by syndromes where the encoder divides.
/// A symbol both agree on is not agreed on by one piece of code twice.
/// </para>
/// It reads level M symbols holding one byte-mode segment, which is all the
/// encoder makes, and throws on anything else or on any inconsistency.
/// </summary>
internal static class QrTestReader
{
    internal sealed record Reading(int Version, int Mask, byte[] Data, byte[] DataCodewords, byte[] AllCodewords);

    // ISO/IEC 18004 table 1 (codewords in the symbol) and table 7 (data codewords at level M), versions 1 to 40.
    private static readonly int[] TotalCodewords =
    [
        26, 44, 70, 100, 134, 172, 196, 242, 292, 346, 404, 466, 532, 581, 655, 733, 815, 901, 991, 1085,
        1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185, 2323, 2465, 2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706
    ];

    private static readonly int[] DataCodewordsAtM =
    [
        16, 28, 44, 64, 86, 108, 124, 154, 182, 216, 254, 290, 334, 365, 415, 453, 507, 563, 627, 669,
        714, 782, 860, 914, 1000, 1062, 1128, 1193, 1267, 1373, 1455, 1541, 1631, 1725, 1812, 1914, 1992, 2102, 2216, 2334
    ];

    // ISO/IEC 18004 table 9: error correction blocks at level M.
    private static readonly int[] BlocksAtM =
    [
        1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16,
        17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49
    ];

    private static readonly int[] Exp = new int[512];
    private static readonly int[] Log = new int[256];

    static QrTestReader()
    {
        var x = 1;
        for (var i = 0; i < 255; i++)
        {
            Exp[i] = x;
            Log[x] = i;
            x <<= 1;
            if (x >= 256)
            {
                x ^= 0x11D;
            }
        }

        for (var i = 255; i < 512; i++)
        {
            Exp[i] = Exp[i - 255];
        }
    }

    public static Reading Read(bool[,] dark)
    {
        var size = dark.GetLength(0);
        Assert.Equal(size, dark.GetLength(1));
        Assert.True(size >= 21 && (size - 17) % 4 == 0, $"{size} is not the side of a QR Code symbol");
        var version = (size - 17) / 4;

        var mask = ReadFormatInformation(dark, size);
        if (version >= 7)
        {
            Assert.Equal(version, ReadVersionInformation(dark, size));
        }

        var function = FunctionModules(version, size);
        Assert.True(dark[size - 8, 8], "the dark module is dark");

        // The codewords, out of the modules that are left: two columns at a time from the right,
        // up then down, right before left, skipping the timing column.
        var total = TotalCodewords[version - 1];
        var bits = new List<bool>(total * 8);
        var upward = true;
        for (var right = size - 1; right > 0; right -= 2)
        {
            if (right == 6)
            {
                right--;
            }

            for (var n = 0; n < size; n++)
            {
                var row = upward ? size - 1 - n : n;
                for (var column = right; column >= right - 1; column--)
                {
                    if (!function[row, column])
                    {
                        bits.Add(dark[row, column] ^ Masked(mask, row, column));
                    }
                }
            }

            upward = !upward;
        }

        // What is left over after the last codeword is at most seven modules, and they carry zeros.
        Assert.InRange(bits.Count - (total * 8), 0, 7);
        Assert.All(bits.Skip(total * 8), bit => Assert.False(bit));
        var codewords = new byte[total];
        for (var i = 0; i < total * 8; i++)
        {
            if (bits[i])
            {
                codewords[i / 8] |= (byte)(0x80 >> (i % 8));
            }
        }

        // Take the blocks apart again. The shorter blocks come first.
        var blocks = BlocksAtM[version - 1];
        var dataTotal = DataCodewordsAtM[version - 1];
        var eccPerBlock = (total - dataTotal) / blocks;
        Assert.Equal(total - dataTotal, eccPerBlock * blocks);
        var shortData = dataTotal / blocks;
        var longBlocks = dataTotal % blocks;
        var dataLengths = Enumerable.Range(0, blocks).Select(b => shortData + (b >= blocks - longBlocks ? 1 : 0)).ToArray();
        var blockData = dataLengths.Select(length => new byte[length]).ToArray();
        var blockEcc = Enumerable.Range(0, blocks).Select(_ => new byte[eccPerBlock]).ToArray();
        var at = 0;
        for (var i = 0; i < shortData + 1; i++)
        {
            for (var b = 0; b < blocks; b++)
            {
                if (i < dataLengths[b])
                {
                    blockData[b][i] = codewords[at++];
                }
            }
        }

        for (var i = 0; i < eccPerBlock; i++)
        {
            for (var b = 0; b < blocks; b++)
            {
                blockEcc[b][i] = codewords[at++];
            }
        }

        Assert.Equal(total, at);

        // Every block is a Reed-Solomon codeword: the polynomial it spells has 2^0 ... 2^(ecc - 1) as roots.
        for (var b = 0; b < blocks; b++)
        {
            var block = blockData[b].Concat(blockEcc[b]).ToArray();
            for (var root = 0; root < eccPerBlock; root++)
            {
                var value = 0;
                foreach (var coefficient in block)
                {
                    value = Times(value, Exp[root]) ^ coefficient;
                }

                Assert.True(value == 0, $"block {b + 1} of {blocks} fails the error correction check at root {root}");
            }
        }

        var data = blockData.SelectMany(block => block).ToArray();
        return new Reading(version, mask, ReadByteSegment(data, version), data, codewords);
    }

    /// <summary>The modules of an SVG path made of "M x y h w v1 h-w z" rectangles, less the quiet zone.</summary>
    public static bool[,] FromSvgPath(string pathData, int side, int quietZone)
    {
        var size = side - (2 * quietZone);
        var dark = new bool[size, size];
        var rectangles = System.Text.RegularExpressions.Regex.Matches(pathData, @"M(\d+) (\d+)h(\d+)v1h-(\d+)z");
        Assert.Equal(pathData.Length, rectangles.Sum(r => r.Length));
        foreach (System.Text.RegularExpressions.Match r in rectangles)
        {
            var x = int.Parse(r.Groups[1].Value);
            var y = int.Parse(r.Groups[2].Value);
            var width = int.Parse(r.Groups[3].Value);
            Assert.Equal(r.Groups[3].Value, r.Groups[4].Value);
            Assert.True(width > 0);
            for (var i = 0; i < width; i++)
            {
                // Nothing is drawn into the quiet zone, and no module twice.
                Assert.InRange(x + i, quietZone, quietZone + size - 1);
                Assert.InRange(y, quietZone, quietZone + size - 1);
                Assert.False(dark[y - quietZone, x + i - quietZone]);
                dark[y - quietZone, x + i - quietZone] = true;
            }
        }

        return dark;
    }

    public static bool[,] ModulesOf(Dealoware.Api.Admin.AdminQrCode code)
    {
        var dark = new bool[code.Size, code.Size];
        for (var row = 0; row < code.Size; row++)
        {
            for (var column = 0; column < code.Size; column++)
            {
                dark[row, column] = code.IsDark(row, column);
            }
        }

        return dark;
    }

    /// <summary>Rows of '1' (dark) and '0' (light), top to bottom.</summary>
    public static string[] RowsOf(bool[,] dark)
    {
        var size = dark.GetLength(0);
        var rows = new string[size];
        for (var row = 0; row < size; row++)
        {
            rows[row] = string.Create(size, (dark, row), static (span, state) =>
            {
                for (var column = 0; column < span.Length; column++)
                {
                    span[column] = state.dark[state.row, column] ? '1' : '0';
                }
            });
        }

        return rows;
    }

    /// <summary>
    /// The alignment pattern centres of a version, by the standard's rule:
    /// the first at 6, the last 7 in from the far edge, the rest evenly
    /// spaced on even numbers, the uneven step first.
    /// </summary>
    public static int[] AlignmentCentres(int version)
    {
        if (version == 1)
        {
            return [];
        }

        var count = (version / 7) + 2;
        var last = (4 * version) + 10;
        // The gap between two centres: the span divided evenly, rounded up to an even number.
        // Version 32 is the one place this rule and the standard's table part ways.
        var even = ((last - 6) + (count - 2)) / (count - 1);
        var step = version == 32 ? 26 : (even + 1) / 2 * 2;
        var centres = new int[count];
        centres[0] = 6;
        for (var i = count - 1; i >= 1; i--)
        {
            centres[i] = last - ((count - 1 - i) * step);
        }

        return centres;
    }

    private static int ReadFormatInformation(bool[,] dark, int size)
    {
        // Bit 14 first. Around the top-left finder: along row 8 from the left, then up column 8.
        (int Row, int Column)[] first =
        [
            (8, 0), (8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 7), (8, 8),
            (7, 8), (5, 8), (4, 8), (3, 8), (2, 8), (1, 8), (0, 8)
        ];
        // The second copy: up column 8 from the bottom edge, then along row 8 to the right edge.
        (int Row, int Column)[] second =
        [
            (size - 1, 8), (size - 2, 8), (size - 3, 8), (size - 4, 8), (size - 5, 8), (size - 6, 8), (size - 7, 8),
            (8, size - 8), (8, size - 7), (8, size - 6), (8, size - 5), (8, size - 4), (8, size - 3), (8, size - 2), (8, size - 1)
        ];

        var a = Bits(dark, first);
        var b = Bits(dark, second);
        Assert.True(a == b, "the two copies of the format information agree");

        var unmasked = a ^ 0b101010000010010;
        // A (15,5) BCH codeword leaves no remainder when divided by x^10 + x^8 + x^5 + x^4 + x^2 + x + 1.
        var remainder = unmasked;
        for (var bit = 14; bit >= 10; bit--)
        {
            if (((remainder >> bit) & 1) != 0)
            {
                remainder ^= 0b10100110111 << (bit - 10);
            }
        }

        Assert.True(remainder == 0, "the format information passes its BCH check");
        Assert.True((unmasked >> 13) == 0b00, "the format information says level M");
        return (unmasked >> 10) & 0b111;
    }

    private static int ReadVersionInformation(bool[,] dark, int size)
    {
        var bottomLeft = 0;
        var topRight = 0;
        for (var i = 17; i >= 0; i--)
        {
            // Bit i sits at row (size - 11 + i mod 3), column (i div 3), and mirrored.
            bottomLeft = (bottomLeft << 1) | (dark[size - 11 + (i % 3), i / 3] ? 1 : 0);
            topRight = (topRight << 1) | (dark[i / 3, size - 11 + (i % 3)] ? 1 : 0);
        }

        Assert.True(bottomLeft == topRight, "the two copies of the version information agree");

        // An (18,6) Golay codeword leaves no remainder when divided by 1111100100101.
        var remainder = bottomLeft;
        for (var bit = 17; bit >= 12; bit--)
        {
            if (((remainder >> bit) & 1) != 0)
            {
                remainder ^= 0b1111100100101 << (bit - 12);
            }
        }

        Assert.True(remainder == 0, "the version information passes its check");
        return bottomLeft >> 12;
    }

    private static int Bits(bool[,] dark, (int Row, int Column)[] places)
    {
        var value = 0;
        foreach (var (row, column) in places)
        {
            value = (value << 1) | (dark[row, column] ? 1 : 0);
        }

        return value;
    }

    private static bool[,] FunctionModules(int version, int size)
    {
        var function = new bool[size, size];

        void Mark(int top, int left, int height, int width)
        {
            for (var row = top; row < top + height; row++)
            {
                for (var column = left; column < left + width; column++)
                {
                    function[row, column] = true;
                }
            }
        }

        // The three finder patterns with their separators and the format information beside them.
        Mark(0, 0, 9, 9);
        Mark(0, size - 8, 9, 8);
        Mark(size - 8, 0, 8, 9);
        // The timing patterns.
        Mark(6, 0, 1, size);
        Mark(0, 6, size, 1);

        var centres = AlignmentCentres(version);
        foreach (var row in centres)
        {
            foreach (var column in centres)
            {
                // Not where a finder pattern stands.
                if ((row == 6 && column == 6) || (row == 6 && column == size - 7) || (row == size - 7 && column == 6))
                {
                    continue;
                }

                Mark(row - 2, column - 2, 5, 5);
            }
        }

        if (version >= 7)
        {
            Mark(size - 11, 0, 3, 6);
            Mark(0, size - 11, 6, 3);
        }

        return function;
    }

    private static bool Masked(int mask, int i, int j)
        => mask switch
        {
            0b000 => (i + j) % 2 == 0,
            0b001 => i % 2 == 0,
            0b010 => j % 3 == 0,
            0b011 => (i + j) % 3 == 0,
            0b100 => ((i / 2) + (j / 3)) % 2 == 0,
            0b101 => ((i * j) % 2) + ((i * j) % 3) == 0,
            0b110 => (((i * j) % 2) + ((i * j) % 3)) % 2 == 0,
            0b111 => (((i + j) % 2) + ((i * j) % 3)) % 2 == 0,
            _ => throw new ArgumentOutOfRangeException(nameof(mask))
        };

    private static int Times(int a, int b)
        => a == 0 || b == 0 ? 0 : Exp[Log[a] + Log[b]];

    private static byte[] ReadByteSegment(byte[] data, int version)
    {
        var position = 0;

        int Take(int count)
        {
            var value = 0;
            for (var i = 0; i < count; i++, position++)
            {
                value = (value << 1) | ((data[position / 8] >> (7 - (position % 8))) & 1);
            }

            return value;
        }

        Assert.True(Take(4) == 0b0100, "the symbol opens with the byte mode indicator");
        var count = Take(version <= 9 ? 8 : 16);
        var bytes = new byte[count];
        for (var i = 0; i < count; i++)
        {
            bytes[i] = (byte)Take(8);
        }

        // Then the terminator (as much of it as fits), zeros to the end of the codeword, and the two pad codewords by turns.
        var capacity = data.Length * 8;
        var terminator = Math.Min(4, capacity - position);
        Assert.Equal(0, Take(terminator));
        while (position % 8 != 0)
        {
            Assert.Equal(0, Take(1));
        }

        for (var pad = 0; position < capacity; pad++)
        {
            Assert.Equal(pad % 2 == 0 ? 0b11101100 : 0b00010001, Take(8));
        }

        return bytes;
    }
}

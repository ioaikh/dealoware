using System.Text;

namespace Dealoware.Api.Admin;

/// <summary>
/// A QR Code symbol (ISO/IEC 18004, the kind every phone camera and
/// authenticator app reads), made by this repository's own code. No library
/// is involved, so the authenticator key passes through nothing but the
/// server's own code on its way into the set-up page.
/// <para>
/// What it does: byte mode, error correction level M, versions 1 to 40 with
/// the smallest that holds the text chosen, all eight masks scored and the
/// best one taken, format information, and version information from
/// version 7 on.
/// </para>
/// <para>
/// What it does not do: the numeric, alphanumeric and kanji modes, the other
/// three error correction levels, ECI, structured append and Micro QR. An
/// otpauth address needs none of them.
/// </para>
/// Rows and columns count from the top left corner, without the quiet zone.
/// </summary>
public sealed class AdminQrCode
{
    public const int MinVersion = 1;
    public const int MaxVersion = 40;
    public const int MaskCount = 8;

    /// <summary>Light modules a reader needs on every side of the symbol.</summary>
    public const int QuietZone = 4;

    // Level M, by version (index 0 is not a version): error correction
    // codewords in each block, and the number of blocks. ISO/IEC 18004 table 9.
    private static readonly byte[] EccCodewordsPerBlock =
    [
        0, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26,
        26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28
    ];

    private static readonly byte[] BlockCounts =
    [
        0, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16,
        17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49
    ];

    // Row and column of the centres of the alignment patterns, by version.
    // ISO/IEC 18004 annex E.
    private static readonly byte[][] AlignmentCentres =
    [
        [], [],
        [6, 18], [6, 22], [6, 26], [6, 30], [6, 34],
        [6, 22, 38], [6, 24, 42], [6, 26, 46], [6, 28, 50], [6, 30, 54], [6, 32, 58], [6, 34, 62],
        [6, 26, 46, 66], [6, 26, 48, 70], [6, 26, 50, 74], [6, 30, 54, 78], [6, 30, 56, 82], [6, 30, 58, 86],
        [6, 34, 62, 90],
        [6, 28, 50, 72, 94], [6, 26, 50, 74, 98], [6, 30, 54, 78, 102], [6, 28, 54, 80, 106],
        [6, 32, 58, 84, 110], [6, 30, 58, 86, 114], [6, 34, 62, 90, 118],
        [6, 26, 50, 74, 98, 122], [6, 30, 54, 78, 102, 126], [6, 26, 52, 78, 104, 130],
        [6, 30, 56, 82, 108, 134], [6, 34, 60, 86, 112, 138], [6, 30, 58, 86, 114, 142],
        [6, 34, 62, 90, 118, 146],
        [6, 30, 54, 78, 102, 126, 150], [6, 24, 50, 76, 102, 128, 154], [6, 28, 54, 80, 106, 132, 158],
        [6, 32, 58, 84, 110, 136, 162], [6, 26, 54, 82, 110, 138, 166], [6, 30, 58, 86, 114, 142, 170]
    ];

    private readonly bool[,] _dark;

    private AdminQrCode(int version, int mask, bool[,] dark)
    {
        Version = version;
        Mask = mask;
        _dark = dark;
    }

    /// <summary>1 to 40. The symbol is 17 + 4 × version modules on a side.</summary>
    public int Version { get; }

    /// <summary>The mask pattern applied to the data, 0 to 7.</summary>
    public int Mask { get; }

    /// <summary>Modules on a side, without the quiet zone.</summary>
    public int Size => SizeOf(Version);

    public bool IsDark(int row, int column) => _dark[row, column];

    /// <summary>The text as UTF-8 bytes, in the smallest version that holds them.</summary>
    public static AdminQrCode Encode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Encode(Encoding.UTF8.GetBytes(text));
    }

    public static AdminQrCode Encode(ReadOnlySpan<byte> data)
    {
        var version = SmallestVersionFor(data.Length);
        return FromDataCodewords(version, DataCodewords(data, version));
    }

    public static int SizeOf(int version)
    {
        RequireVersion(version);
        return 17 + (4 * version);
    }

    /// <summary>How many bytes one symbol of this version holds in byte mode at level M.</summary>
    public static int ByteCapacity(int version)
        // The mode takes 4 bits and the count 8 (16 from version 10): one and a half or two and a half codewords.
        => DataCodewordCount(version) - (version < 10 ? 2 : 3);

    public static int SmallestVersionFor(int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        for (var version = MinVersion; version <= MaxVersion; version++)
        {
            if (byteCount <= ByteCapacity(version))
            {
                return version;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(byteCount), "Too long for one QR Code symbol at level M.");
    }

    /// <summary>Codewords in the symbol, data and error correction together.</summary>
    public static int TotalCodewordCount(int version)
    {
        RequireVersion(version);

        // Every module, less the finder patterns with their separators, the timing
        // patterns, the format information and the dark module...
        var modules = (((16 * version) + 128) * version) + 64;
        if (version >= 2)
        {
            // ...less the alignment patterns...
            var perSide = (version / 7) + 2;
            modules -= (((25 * perSide) - 10) * perSide) - 55;
            if (version >= 7)
            {
                // ...less the two copies of the version information.
                modules -= 36;
            }
        }

        // Up to seven modules are left over. They stay light.
        return modules / 8;
    }

    public static int DataCodewordCount(int version)
        => TotalCodewordCount(version) - (EccCodewordsPerBlock[version] * BlockCounts[version]);

    /// <summary>
    /// The data codewords for these bytes: the byte-mode indicator, the count,
    /// the bytes, the terminator, and padding up to the version's capacity.
    /// </summary>
    public static byte[] DataCodewords(ReadOnlySpan<byte> data, int version)
    {
        if (data.Length > ByteCapacity(version))
        {
            throw new ArgumentException("The data does not fit this version.", nameof(data));
        }

        var codewords = new byte[DataCodewordCount(version)];
        var capacityBits = codewords.Length * 8;
        var length = 0;

        void Append(int value, int bitCount)
        {
            for (var i = bitCount - 1; i >= 0; i--, length++)
            {
                if (((value >> i) & 1) != 0)
                {
                    codewords[length >> 3] |= (byte)(0x80 >> (length & 7));
                }
            }
        }

        Append(0b0100, 4);
        Append(data.Length, version < 10 ? 8 : 16);
        foreach (var b in data)
        {
            Append(b, 8);
        }

        // The terminator: four zero bits, or fewer when the symbol is full sooner.
        length += Math.Min(4, capacityBits - length);
        // Zero bits up to the next whole codeword.
        length += (8 - (length & 7)) & 7;
        // Then the two pad codewords, turn and turn about.
        for (var pad = 0xEC; length < capacityBits; pad ^= 0xEC ^ 0x11)
        {
            Append(pad, 8);
        }

        return codewords;
    }

    /// <summary>
    /// The symbol for data codewords that are already made up. With no mask
    /// named, each of the eight is tried and the one with the lowest penalty
    /// is kept (the lowest number wins a tie).
    /// </summary>
    public static AdminQrCode FromDataCodewords(int version, ReadOnlySpan<byte> dataCodewords, int? mask = null)
    {
        if (dataCodewords.Length != DataCodewordCount(version))
        {
            throw new ArgumentException("Not the number of data codewords this version holds.", nameof(dataCodewords));
        }

        if (mask is < 0 or >= MaskCount)
        {
            throw new ArgumentOutOfRangeException(nameof(mask));
        }

        var size = SizeOf(version);
        var dark = new bool[size, size];
        var function = new bool[size, size];
        DrawFunctionPatterns(version, dark, function);
        DrawCodewords(
            WithErrorCorrection(dataCodewords, BlockCounts[version], EccCodewordsPerBlock[version]),
            dark,
            function);

        var chosen = mask ?? -1;
        if (chosen < 0)
        {
            var lowest = int.MaxValue;
            for (var candidate = 0; candidate < MaskCount; candidate++)
            {
                ApplyMask(candidate, dark, function);
                DrawFormatInformation(candidate, dark, function);
                var penalty = Penalty(dark);
                if (penalty < lowest)
                {
                    lowest = penalty;
                    chosen = candidate;
                }

                // Masking is an exclusive-or: doing it again undoes it.
                ApplyMask(candidate, dark, function);
            }
        }

        ApplyMask(chosen, dark, function);
        DrawFormatInformation(chosen, dark, function);
        return new AdminQrCode(version, chosen, dark);
    }

    /// <summary>
    /// The data codewords split into blocks, each followed up by its
    /// Reed-Solomon codewords, and the blocks woven together: first the data
    /// codewords of every block in turn, then the error correction codewords
    /// the same way. Later blocks are one data codeword longer when the data
    /// does not divide evenly.
    /// </summary>
    public static byte[] WithErrorCorrection(ReadOnlySpan<byte> data, int blockCount, int eccPerBlock)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(blockCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(eccPerBlock, 1);
        if (data.Length < blockCount)
        {
            throw new ArgumentException("Fewer data codewords than blocks.", nameof(data));
        }

        var shortLength = data.Length / blockCount;
        var longBlocks = data.Length % blockCount;
        var shortBlocks = blockCount - longBlocks;
        var blocks = new byte[blockCount][];
        var ecc = new byte[blockCount][];
        var offset = 0;
        for (var i = 0; i < blockCount; i++)
        {
            var length = shortLength + (i < shortBlocks ? 0 : 1);
            blocks[i] = data.Slice(offset, length).ToArray();
            ecc[i] = ReedSolomonRemainder(blocks[i], eccPerBlock);
            offset += length;
        }

        var result = new byte[data.Length + (blockCount * eccPerBlock)];
        var at = 0;
        for (var i = 0; i <= shortLength; i++)
        {
            foreach (var block in blocks)
            {
                if (i < block.Length)
                {
                    result[at++] = block[i];
                }
            }
        }

        for (var i = 0; i < eccPerBlock; i++)
        {
            foreach (var block in ecc)
            {
                result[at++] = block[i];
            }
        }

        return result;
    }

    /// <summary>
    /// The Reed-Solomon error correction codewords for one block: the
    /// remainder of the data, as a polynomial over GF(256), divided by the
    /// generator polynomial whose roots are 2^0 to 2^(degree - 1).
    /// </summary>
    public static byte[] ReedSolomonRemainder(ReadOnlySpan<byte> data, int degree)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(degree, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(degree, 255);

        // The generator polynomial, highest power first, without its leading 1.
        var generator = new byte[degree];
        generator[degree - 1] = 1;
        var root = 1;
        for (var i = 0; i < degree; i++)
        {
            for (var j = 0; j < degree; j++)
            {
                generator[j] = (byte)Multiply(generator[j], root);
                if (j + 1 < degree)
                {
                    generator[j] ^= generator[j + 1];
                }
            }

            root = Multiply(root, 2);
        }

        var remainder = new byte[degree];
        foreach (var b in data)
        {
            var factor = b ^ remainder[0];
            Array.Copy(remainder, 1, remainder, 0, degree - 1);
            remainder[degree - 1] = 0;
            for (var i = 0; i < degree; i++)
            {
                remainder[i] ^= (byte)Multiply(generator[i], factor);
            }
        }

        return remainder;
    }

    /// <summary>
    /// The 15 format information bits for level M and this mask: 5 data bits,
    /// 10 BCH check bits, and the fixed mask the standard lays over them.
    /// </summary>
    public static int FormatInformation(int mask)
    {
        if (mask is < 0 or >= MaskCount)
        {
            throw new ArgumentOutOfRangeException(nameof(mask));
        }

        // Level M is written 00 (L is 01, H is 10, Q is 11).
        const int levelM = 0b00;
        var data = (levelM << 3) | mask;
        var remainder = data;
        for (var i = 0; i < 10; i++)
        {
            remainder = (remainder << 1) ^ ((remainder >> 9) * 0x537);
        }

        return ((data << 10) | remainder) ^ 0x5412;
    }

    /// <summary>The 18 version information bits: 6 for the version and 12 BCH check bits. Versions 7 to 40.</summary>
    public static int VersionInformation(int version)
    {
        RequireVersion(version);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 7);

        var remainder = version;
        for (var i = 0; i < 12; i++)
        {
            remainder = (remainder << 1) ^ ((remainder >> 11) * 0x1F25);
        }

        return (version << 12) | remainder;
    }

    /// <summary>
    /// The dark modules as the "d" of one SVG path, one unit to a module,
    /// moved in by the quiet zone. Draw it on a light square
    /// <see cref="SvgSize"/> units wide and the quiet zone is there.
    /// </summary>
    public string SvgPathData()
    {
        var size = Size;
        var path = new StringBuilder(size * size);
        for (var row = 0; row < size; row++)
        {
            for (var column = 0; column < size; column++)
            {
                if (!_dark[row, column])
                {
                    continue;
                }

                var start = column;
                while (column < size && _dark[row, column])
                {
                    column++;
                }

                // One closed rectangle for each run of dark modules in the row.
                path.Append('M').Append(start + QuietZone).Append(' ').Append(row + QuietZone)
                    .Append('h').Append(column - start).Append("v1h-").Append(column - start).Append('z');
            }
        }

        return path.ToString();
    }

    /// <summary>Side of the square the path sits in: the symbol and the quiet zone on both sides.</summary>
    public int SvgSize => Size + (2 * QuietZone);

    private static void RequireVersion(int version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, MinVersion);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(version, MaxVersion);
    }

    /// <summary>Multiplies two elements of GF(256) as QR Codes define it (x^8 + x^4 + x^3 + x^2 + 1).</summary>
    private static int Multiply(int x, int y)
    {
        var product = 0;
        for (var i = 7; i >= 0; i--)
        {
            product = (product << 1) ^ ((product >> 7) * 0x11D);
            product ^= ((y >> i) & 1) * x;
        }

        return product;
    }

    /// <summary>
    /// Everything that is not data: the timing patterns, the three finder
    /// patterns with their separators, the alignment patterns, the version
    /// information, and the places of the format information and the dark
    /// module. <paramref name="function"/> marks those modules, so the data
    /// and the mask leave them alone.
    /// </summary>
    private static void DrawFunctionPatterns(int version, bool[,] dark, bool[,] function)
    {
        var size = dark.GetLength(0);

        void Set(int row, int column, bool isDark)
        {
            dark[row, column] = isDark;
            function[row, column] = true;
        }

        // Timing patterns: row 6 and column 6, dark on the even places.
        for (var i = 0; i < size; i++)
        {
            Set(6, i, i % 2 == 0);
            Set(i, 6, i % 2 == 0);
        }

        // Finder patterns, each with its light separator: a 7 by 7 square, a light ring, a dark 3 by 3 middle.
        foreach (var (centreRow, centreColumn) in new[] { (3, 3), (3, size - 4), (size - 4, 3) })
        {
            for (var dy = -4; dy <= 4; dy++)
            {
                for (var dx = -4; dx <= 4; dx++)
                {
                    var row = centreRow + dy;
                    var column = centreColumn + dx;
                    if (row < 0 || row >= size || column < 0 || column >= size)
                    {
                        continue;
                    }

                    var ring = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    Set(row, column, ring != 2 && ring != 4);
                }
            }
        }

        // Alignment patterns: 5 by 5, on every crossing of the listed centres but the three a finder pattern covers.
        var centres = AlignmentCentres[version];
        var last = centres.Length - 1;
        for (var i = 0; i <= last; i++)
        {
            for (var j = 0; j <= last; j++)
            {
                if ((i == 0 && j == 0) || (i == 0 && j == last) || (i == last && j == 0))
                {
                    continue;
                }

                for (var dy = -2; dy <= 2; dy++)
                {
                    for (var dx = -2; dx <= 2; dx++)
                    {
                        Set(centres[i] + dy, centres[j] + dx, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
                    }
                }
            }
        }

        // Keeps the format information's places (the right bits are drawn once the mask is known).
        DrawFormatInformation(0, dark, function);

        // Version information, twice: a 6 by 3 block left of the bottom-left finder, and its mirror above the top-right one.
        if (version >= 7)
        {
            var bits = VersionInformation(version);
            for (var i = 0; i < 18; i++)
            {
                var bit = ((bits >> i) & 1) != 0;
                var a = size - 11 + (i % 3);
                var b = i / 3;
                Set(a, b, bit);
                Set(b, a, bit);
            }
        }
    }

    /// <summary>
    /// The format information, twice: around the top-left finder pattern, and
    /// split between the bottom-left and the top-right one. Bit 0 is the last
    /// of the 15. Also the one module that is always dark.
    /// </summary>
    private static void DrawFormatInformation(int mask, bool[,] dark, bool[,] function)
    {
        var size = dark.GetLength(0);
        var bits = FormatInformation(mask);

        void Set(int row, int column, bool isDark)
        {
            dark[row, column] = isDark;
            function[row, column] = true;
        }

        bool Bit(int i) => ((bits >> i) & 1) != 0;

        // First copy: up column 8 from the top (skipping the timing row), then left along row 8 (skipping the timing column).
        for (var i = 0; i <= 5; i++)
        {
            Set(i, 8, Bit(i));
        }

        Set(7, 8, Bit(6));
        Set(8, 8, Bit(7));
        Set(8, 7, Bit(8));
        for (var i = 9; i < 15; i++)
        {
            Set(8, 14 - i, Bit(i));
        }

        // Second copy: row 8 from the right edge, then column 8 down to the bottom edge.
        for (var i = 0; i < 8; i++)
        {
            Set(8, size - 1 - i, Bit(i));
        }

        for (var i = 8; i < 15; i++)
        {
            Set(size - 15 + i, 8, Bit(i));
        }

        Set(size - 8, 8, true);
    }

    /// <summary>
    /// Lays the codewords into the modules that are free: in columns two
    /// modules wide from the right edge, upwards and downwards by turns,
    /// right module before left, most significant bit first. The timing
    /// column is stepped over.
    /// </summary>
    private static void DrawCodewords(byte[] codewords, bool[,] dark, bool[,] function)
    {
        var size = dark.GetLength(0);
        var bit = 0;
        var bitCount = codewords.Length * 8;
        var upward = true;
        for (var right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
            {
                right = 5;
            }

            for (var step = 0; step < size; step++)
            {
                var row = upward ? size - 1 - step : step;
                for (var j = 0; j < 2; j++)
                {
                    var column = right - j;
                    if (function[row, column] || bit >= bitCount)
                    {
                        continue;
                    }

                    dark[row, column] = ((codewords[bit >> 3] >> (7 - (bit & 7))) & 1) != 0;
                    bit++;
                }
            }

            upward = !upward;
        }
    }

    /// <summary>Turns over every data module the mask pattern picks out.</summary>
    private static void ApplyMask(int mask, bool[,] dark, bool[,] function)
    {
        var size = dark.GetLength(0);
        for (var row = 0; row < size; row++)
        {
            for (var column = 0; column < size; column++)
            {
                if (!function[row, column] && MaskPicks(mask, row, column))
                {
                    dark[row, column] = !dark[row, column];
                }
            }
        }
    }

    private static bool MaskPicks(int mask, int row, int column)
        => mask switch
        {
            0 => (row + column) % 2 == 0,
            1 => row % 2 == 0,
            2 => column % 3 == 0,
            3 => (row + column) % 3 == 0,
            4 => ((row / 2) + (column / 3)) % 2 == 0,
            5 => ((row * column) % 2) + ((row * column) % 3) == 0,
            6 => (((row * column) % 2) + ((row * column) % 3)) % 2 == 0,
            _ => (((row + column) % 2) + ((row * column) % 3)) % 2 == 0
        };

    /// <summary>
    /// The standard's four penalties for a masked symbol; the lower the sum,
    /// the easier the symbol is to read. Which mask wins never decides
    /// whether a symbol can be read: every mask gives a valid one.
    /// </summary>
    private static int Penalty(bool[,] dark)
    {
        var size = dark.GetLength(0);
        var penalty = 0;

        // 1: five or more modules of one colour in a line, by row and by column.
        // 3: the finder pattern's 1:1:3:1:1 with four light modules before or after it.
        for (var line = 0; line < size; line++)
        {
            for (var byRow = 0; byRow < 2; byRow++)
            {
                var run = 0;
                var runDark = false;
                var window = 0;
                for (var i = 0; i < size; i++)
                {
                    var isDark = byRow == 0 ? dark[line, i] : dark[i, line];
                    if (i > 0 && isDark == runDark)
                    {
                        run++;
                        if (run == 5)
                        {
                            penalty += 3;
                        }
                        else if (run > 5)
                        {
                            penalty++;
                        }
                    }
                    else
                    {
                        run = 1;
                        runDark = isDark;
                    }

                    // The last eleven modules of the line as bits.
                    window = ((window << 1) | (isDark ? 1 : 0)) & 0x7FF;
                    if (i >= 10 && (window == 0b10111010000 || window == 0b00001011101))
                    {
                        penalty += 40;
                    }
                }
            }
        }

        // 2: every 2 by 2 square of one colour.
        var darkCount = 0;
        for (var row = 0; row < size; row++)
        {
            for (var column = 0; column < size; column++)
            {
                var isDark = dark[row, column];
                if (isDark)
                {
                    darkCount++;
                }

                if (row + 1 < size && column + 1 < size
                    && isDark == dark[row, column + 1]
                    && isDark == dark[row + 1, column]
                    && isDark == dark[row + 1, column + 1])
                {
                    penalty += 3;
                }
            }
        }

        // 4: how far the share of dark modules is from one half, in steps of five in a hundred.
        var total = size * size;
        penalty += Math.Abs((darkCount * 2) - total) * 10 / total * 10;
        return penalty;
    }
}

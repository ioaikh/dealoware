using System.Security.Cryptography;
using System.Text;
using Dealoware.Api.Admin;

namespace Dealoware.Api.Tests;

/// <summary>
/// The QR Code encoder behind the authenticator set-up page
/// (<see cref="AdminQrCode"/>). A symbol no authenticator app can read would
/// stop the owner at first sign-in, so the encoder is held against answers
/// that do not come from itself:
/// <list type="bullet">
/// <item>the codewords printed in ISO/IEC 18004 annex I ("01234567", version
/// 1-M) and in the Thonky QR Code tutorial ("HELLO WORLD", version 1-M; the
/// four blocks of its version 5-Q example and their interleaving);</item>
/// <item>the standard's tables: the eight format information strings of
/// level M, the 34 version information strings, and the byte capacities of
/// all 40 versions;</item>
/// <item>whole symbols pinned module by module, for versions 1, 5, 7 and 10.
/// Their expected modules were not taken from this encoder: they were made
/// with two other encoders (python-qrcode 8.2 and segno 1.6.6) when this
/// file was written, and are kept here as text. Neither library is used by
/// the repository or by these tests;</item>
/// <item>a reader written separately from the standard
/// (<see cref="QrTestReader"/>), which must get the text back out of every
/// symbol, with every error correction block checking out.</item>
/// </list>
/// </summary>
public class AdminQrCodeTests
{
    // ISO/IEC 18004 annex I: "01234567" at version 1-M. Sixteen data codewords, ten for error correction.
    private static readonly byte[] IsoAnnexData =
        [0x10, 0x20, 0x0C, 0x56, 0x61, 0x80, 0xEC, 0x11, 0xEC, 0x11, 0xEC, 0x11, 0xEC, 0x11, 0xEC, 0x11];

    private static readonly byte[] IsoAnnexEcc = [0xA5, 0x24, 0xD4, 0xC1, 0xED, 0x36, 0xC7, 0x87, 0x2C, 0x55];

    // Those codewords as a symbol with mask pattern 010, as python-qrcode and segno both draw it.
    private static readonly string[] IsoAnnexSymbolMask2 =
    [
        "111111100101101111111",
        "100000100111101000001",
        "101110101000001011101",
        "101110101100001011101",
        "101110101011101011101",
        "100000101000101000001",
        "111111101010101111111",
        "000000001001100000000",
        "101111100100101111100",
        "000101011010100101100",
        "001000110101010011111",
        "000010000100000111100",
        "000111111001010010000",
        "000000001011111001100",
        "111111100110101100000",
        "100000101011111000101",
        "101110101000100101100",
        "101110101100100100000",
        "101110101011010010100",
        "100000100000000110110",
        "111111101111010010100"
    ];

    // The example of the "Key URI Format" page that authenticator apps follow.
    private const string KeyUriExample =
        "otpauth://totp/Example:alice@google.com?secret=JBSWY3DPEHPK3PXP&issuer=Example";

    // That text at version 5-M with mask pattern 100, as python-qrcode draws it.
    private static readonly string[] KeyUriSymbolMask4 =
    [
        "1111111011110111111001110110101111111",
        "1000001000011011100110100100001000001",
        "1011101000111011110001100011101011101",
        "1011101010110001101000010111101011101",
        "1011101010110000111000010000101011101",
        "1000001011000000000100100010101000001",
        "1111111010101010101010101010101111111",
        "0000000011010101110111010010000000000",
        "1000101111010011110101100011111111001",
        "0100100001100000001001010000111011010",
        "1010111111011011011011111001101110100",
        "0001000001101101111011000001101100110",
        "0011111000011100011011101001001101111",
        "1111010000011001101000010111000110000",
        "1111011001001101011011010101111011100",
        "1011100101011100111111000011100010111",
        "1111011101001010001011110100001010101",
        "0010000011101010111000110100110011010",
        "0010101011011000101010111001001111100",
        "1000110001010010010001010011001001101",
        "0101101000000001110111110000111001110",
        "1000100011100011111000011111110110010",
        "0100101111110011001010111001110000100",
        "0101010000101100000111111001001010101",
        "0000101110000111100011001100011011101",
        "1100110111011100001000110000101011100",
        "0011101011101001000001010011100010100",
        "0000100101101101111001001011110000111",
        "1100111001000100110010101000111110010",
        "0000000011000101001001010001100011000",
        "1111111010100001010011010000101010100",
        "1000001001010001011111011000100010100",
        "1011101010010101001100111100111111111",
        "1011101001100110110000011101111100101",
        "1011101001110110101011110000011101000",
        "1000001001101100011011110000100000110",
        "1111111011000111010111101001001010011"
    ];

    // Cut to 122 and to 213 bytes it is exactly what versions 7 and 10 hold, so those symbols carry no padding.
    private const string TwoOtpAuthAddresses =
        "otpauth://totp/Dealoware%3Aowner%40example.test?secret=JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP&issuer=Dealoware&digits=6&period=30&algorithm=SHA1"
        + "otpauth://totp/Dealoware%3Aowner%40example.test?secret=JBSWY3DPEHPK3PXPJBSW";

    [Fact]
    public void ReedSolomon_GivesTheErrorCorrectionCodewordsOfTheStandardsOwnExample()
    {
        Assert.Equal(IsoAnnexEcc, AdminQrCode.ReedSolomonRemainder(IsoAnnexData, 10));

        // One block at version 1-M: the data, then its error correction codewords.
        Assert.Equal(IsoAnnexData.Concat(IsoAnnexEcc).ToArray(), AdminQrCode.WithErrorCorrection(IsoAnnexData, 1, 10));
    }

    [Fact]
    public void ReedSolomon_GivesTheErrorCorrectionCodewordsOfThePublishedHelloWorldExample()
    {
        // Thonky tutorial, "HELLO WORLD" at 1-M.
        byte[] data = [32, 91, 11, 120, 209, 114, 220, 77, 67, 64, 236, 17, 236, 17, 236, 17];
        byte[] ecc = [196, 35, 39, 119, 235, 215, 231, 226, 93, 23];

        Assert.Equal(ecc, AdminQrCode.ReedSolomonRemainder(data, 10));
    }

    [Fact]
    public void Blocks_AreSplitCorrectedAndInterleaved_AsInThePublishedVersion5Example()
    {
        // Thonky tutorial, version 5-Q: two blocks of 15 data codewords, then two of 16, 18 error
        // correction codewords each. The level differs from ours; the block arithmetic is the same.
        byte[][] data =
        [
            [67, 85, 70, 134, 87, 38, 85, 194, 119, 50, 6, 18, 6, 103, 38],
            [246, 246, 66, 7, 118, 134, 242, 7, 38, 86, 22, 198, 199, 146, 6],
            [182, 230, 247, 119, 50, 7, 118, 134, 87, 38, 82, 6, 134, 151, 50, 7],
            [70, 247, 118, 86, 194, 6, 151, 50, 16, 236, 17, 236, 17, 236, 17, 236]
        ];
        byte[][] ecc =
        [
            [213, 199, 11, 45, 115, 247, 241, 223, 229, 248, 154, 117, 154, 111, 86, 161, 111, 39],
            [87, 204, 96, 60, 202, 182, 124, 157, 200, 134, 27, 129, 209, 17, 163, 163, 120, 133],
            [148, 116, 177, 212, 76, 133, 75, 242, 238, 76, 195, 230, 189, 10, 108, 240, 192, 141],
            [235, 159, 5, 173, 24, 147, 59, 33, 106, 40, 255, 172, 82, 2, 131, 32, 178, 236]
        ];
        byte[] interleaved =
        [
            67, 246, 182, 70, 85, 246, 230, 247, 70, 66, 247, 118, 134, 7, 119, 86, 87, 118, 50, 194, 38, 134, 7, 6,
            85, 242, 118, 151, 194, 7, 134, 50, 119, 38, 87, 16, 50, 86, 38, 236, 6, 22, 82, 17, 18, 198, 6, 236, 6,
            199, 134, 17, 103, 146, 151, 236, 38, 6, 50, 17, 7, 236, 213, 87, 148, 235, 199, 204, 116, 159, 11, 96,
            177, 5, 45, 60, 212, 173, 115, 202, 76, 24, 247, 182, 133, 147, 241, 124, 75, 59, 223, 157, 242, 33, 229,
            200, 238, 106, 248, 134, 76, 40, 154, 27, 195, 255, 117, 129, 230, 172, 154, 209, 189, 82, 111, 17, 10, 2,
            86, 163, 108, 131, 161, 163, 240, 32, 111, 120, 192, 178, 39, 133, 141, 236
        ];

        for (var block = 0; block < data.Length; block++)
        {
            Assert.Equal(ecc[block], AdminQrCode.ReedSolomonRemainder(data[block], 18));
        }

        Assert.Equal(interleaved, AdminQrCode.WithErrorCorrection(data.SelectMany(b => b).ToArray(), 4, 18));
    }

    [Fact]
    public void FormatInformation_IsTheStandardsTableForLevelM()
    {
        // ISO/IEC 18004 annex C: the masked 15-bit strings for level M, mask patterns 000 to 111.
        string[] published =
        [
            "101010000010010", "101000100100101", "101111001111100", "101101101001011",
            "100010111111001", "100000011001110", "100111110010111", "100101010100000"
        ];

        for (var mask = 0; mask < AdminQrCode.MaskCount; mask++)
        {
            Assert.Equal(published[mask], Convert.ToString(AdminQrCode.FormatInformation(mask), 2).PadLeft(15, '0'));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => AdminQrCode.FormatInformation(8));
        Assert.Throws<ArgumentOutOfRangeException>(() => AdminQrCode.FormatInformation(-1));
    }

    [Fact]
    public void VersionInformation_IsTheStandardsTableForVersions7To40()
    {
        // ISO/IEC 18004 annex D.
        int[] published =
        [
            0x07C94, 0x085BC, 0x09A99, 0x0A4D3, 0x0BBF6, 0x0C762, 0x0D847, 0x0E60D, 0x0F928, 0x10B78, 0x1145D, 0x12A17,
            0x13532, 0x149A6, 0x15683, 0x168C9, 0x177EC, 0x18EC4, 0x191E1, 0x1AFAB, 0x1B08E, 0x1CC1A, 0x1D33F, 0x1ED75,
            0x1F250, 0x209D5, 0x216F0, 0x228BA, 0x2379F, 0x24B0B, 0x2542E, 0x26A64, 0x27541, 0x28C69
        ];

        for (var version = 7; version <= AdminQrCode.MaxVersion; version++)
        {
            Assert.Equal(published[version - 7], AdminQrCode.VersionInformation(version));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => AdminQrCode.VersionInformation(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => AdminQrCode.VersionInformation(41));
    }

    [Fact]
    public void ByteCapacity_IsThePublishedCapacityOfEveryVersionAtLevelM_AndTheSmallestThatFitsIsChosen()
    {
        // ISO/IEC 18004 table 7, the "8-bit byte" column at level M.
        int[] published =
        [
            14, 26, 42, 62, 84, 106, 122, 152, 180, 213, 251, 287, 331, 362, 412, 450, 504, 560, 624, 666,
            711, 779, 857, 911, 997, 1059, 1125, 1190, 1264, 1370, 1452, 1538, 1628, 1722, 1809, 1911, 1989, 2099, 2213, 2331
        ];

        for (var version = AdminQrCode.MinVersion; version <= AdminQrCode.MaxVersion; version++)
        {
            var capacity = published[version - 1];
            Assert.Equal(capacity, AdminQrCode.ByteCapacity(version));
            Assert.Equal(17 + (4 * version), AdminQrCode.SizeOf(version));
            Assert.Equal(version, AdminQrCode.SmallestVersionFor(capacity));
            if (version < AdminQrCode.MaxVersion)
            {
                Assert.Equal(version + 1, AdminQrCode.SmallestVersionFor(capacity + 1));
            }
        }

        Assert.Equal(1, AdminQrCode.SmallestVersionFor(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => AdminQrCode.SmallestVersionFor(2332));
        Assert.Throws<ArgumentOutOfRangeException>(() => AdminQrCode.Encode(new byte[2332]));
        Assert.Throws<ArgumentOutOfRangeException>(() => AdminQrCode.ByteCapacity(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => AdminQrCode.ByteCapacity(41));
    }

    [Fact]
    public void Version1_TheStandardsOwnExampleCodewords_AreDrawnModuleForModule()
    {
        var code = AdminQrCode.FromDataCodewords(1, IsoAnnexData, mask: 2);

        Assert.Equal(1, code.Version);
        Assert.Equal(21, code.Size);
        Assert.Equal(2, code.Mask);
        Assert.Equal(IsoAnnexSymbolMask2, QrTestReader.RowsOf(QrTestReader.ModulesOf(code)));
    }

    [Fact]
    public void Version5_ThePublishedOtpAuthExample_IsDrawnModuleForModule_AndReadsBack()
    {
        var code = AdminQrCode.Encode(KeyUriExample);

        Assert.Equal(78, Encoding.UTF8.GetByteCount(KeyUriExample));
        Assert.Equal(5, code.Version);
        Assert.Equal(37, code.Size);
        // The mask this encoder picks is also the one python-qrcode's scoring rates best of these eight symbols.
        Assert.Equal(4, code.Mask);
        Assert.Equal(KeyUriSymbolMask4, QrTestReader.RowsOf(QrTestReader.ModulesOf(code)));

        // Byte mode, a count of 78, the text, the terminator and three pairs of pad codewords.
        var data = AdminQrCode.DataCodewords(Encoding.UTF8.GetBytes(KeyUriExample), 5);
        Assert.Equal(86, data.Length);
        Assert.Equal(
            "44E" + Convert.ToHexString(Encoding.UTF8.GetBytes(KeyUriExample)) + "0" + "EC11EC11EC11",
            Convert.ToHexString(data));

        var reading = QrTestReader.Read(QrTestReader.ModulesOf(code));
        Assert.Equal(KeyUriExample, Encoding.UTF8.GetString(reading.Data));
        Assert.Equal(data, reading.DataCodewords);
    }

    [Theory]
    // Version 7 is the first with version information; version 10 the first with a 16-bit count
    // and blocks of two lengths. Both expected symbols are the same from python-qrcode and from segno.
    [InlineData(122, 7, 4, "b29f40dc86c16ba5005df400788b789314266181c79c8d3e66086f2f90ff03ea")]
    [InlineData(213, 10, 6, "1c65d8578e28121a7cb1955a04b33a6b8325a3371704c0423d04e6ec3e4072ce")]
    public void Versions7And10_FullSymbols_AreDrawnAsTwoOtherEncodersDrawThem(int length, int version, int mask, string sha256OfRows)
    {
        var text = TwoOtpAuthAddresses[..length];
        Assert.Equal(length, AdminQrCode.ByteCapacity(version));

        var code = AdminQrCode.Encode(text);

        Assert.Equal(version, code.Version);
        Assert.Equal(mask, code.Mask);
        var rows = string.Join('/', QrTestReader.RowsOf(QrTestReader.ModulesOf(code)));
        Assert.Equal(sha256OfRows, Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(rows))));
        Assert.Equal(text, Encoding.UTF8.GetString(QrTestReader.Read(QrTestReader.ModulesOf(code)).Data));
    }

    [Fact]
    public void AnOtpAuthAddress_HasEveryFixedPatternInPlace()
    {
        // What the set-up page encodes for an owner with an ordinary address: 136 bytes, version 8.
        var text = "otpauth://totp/Dealoware%3Aio%40aiknowhow.com?secret=" + new string('A', 32)
                   + "&issuer=Dealoware&digits=6&period=30&algorithm=SHA1";
        var code = AdminQrCode.Encode(text);
        var size = code.Size;

        Assert.Equal(136, text.Length);
        Assert.Equal(8, code.Version);
        Assert.Equal(49, size);
        Assert.InRange(code.Mask, 0, 7);

        // Three finder patterns: a dark 7 by 7 ring, a light ring, a dark 3 by 3 middle, and a light separator around.
        foreach (var (top, left) in new[] { (0, 0), (0, size - 7), (size - 7, 0) })
        {
            for (var dy = -1; dy <= 7; dy++)
            {
                for (var dx = -1; dx <= 7; dx++)
                {
                    var row = top + dy;
                    var column = left + dx;
                    if (row < 0 || row >= size || column < 0 || column >= size)
                    {
                        continue;
                    }

                    var ring = Math.Max(Math.Abs(dx - 3), Math.Abs(dy - 3));
                    Assert.True(
                        code.IsDark(row, column) == (ring is 0 or 1 or 3),
                        $"finder pattern at {top},{left}: module {row},{column}");
                }
            }
        }

        // Timing patterns: row 6 and column 6 between the finder patterns, dark on the even places.
        for (var i = 8; i < size - 8; i++)
        {
            Assert.Equal(i % 2 == 0, code.IsDark(6, i));
            Assert.Equal(i % 2 == 0, code.IsDark(i, 6));
        }

        // Alignment patterns of version 8: centres at 6, 24 and 42, except where a finder pattern stands.
        Assert.Equal([6, 24, 42], QrTestReader.AlignmentCentres(8));
        foreach (var (row, column) in new[] { (6, 24), (24, 6), (24, 24), (24, 42), (42, 24), (42, 42) })
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                for (var dx = -2; dx <= 2; dx++)
                {
                    Assert.Equal(Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1, code.IsDark(row + dy, column + dx));
                }
            }
        }

        // The module that is always dark.
        Assert.True(code.IsDark(size - 8, 8));

        // Format information, both copies, most significant bit first: level M and the mask the symbol says it used.
        var format = Convert.ToString(AdminQrCode.FormatInformation(code.Mask), 2).PadLeft(15, '0');
        (int Row, int Column)[] aroundTopLeft =
        [
            (8, 0), (8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 7), (8, 8),
            (7, 8), (5, 8), (4, 8), (3, 8), (2, 8), (1, 8), (0, 8)
        ];
        (int Row, int Column)[] splitInTwo =
        [
            (size - 1, 8), (size - 2, 8), (size - 3, 8), (size - 4, 8), (size - 5, 8), (size - 6, 8), (size - 7, 8),
            (8, size - 8), (8, size - 7), (8, size - 6), (8, size - 5), (8, size - 4), (8, size - 3), (8, size - 2), (8, size - 1)
        ];
        Assert.Equal(format, string.Concat(aroundTopLeft.Select(p => code.IsDark(p.Row, p.Column) ? '1' : '0')));
        Assert.Equal(format, string.Concat(splitInTwo.Select(p => code.IsDark(p.Row, p.Column) ? '1' : '0')));
        // Every level M string starts 10: the level's 00 under the fixed mask's 10.
        Assert.StartsWith("10", format, StringComparison.Ordinal);

        // Version information, both copies: 001000 010110111100 for version 8, least significant bit first.
        const int versionEight = 0x085BC;
        for (var i = 0; i < 18; i++)
        {
            var bit = ((versionEight >> i) & 1) != 0;
            Assert.Equal(bit, code.IsDark(size - 11 + (i % 3), i / 3));
            Assert.Equal(bit, code.IsDark(i / 3, size - 11 + (i % 3)));
        }

        // And a reader gets the text back, with every block passing its error correction check.
        var reading = QrTestReader.Read(QrTestReader.ModulesOf(code));
        Assert.Equal(text, Encoding.UTF8.GetString(reading.Data));
        Assert.Equal(8, reading.Version);
        Assert.Equal(code.Mask, reading.Mask);
    }

    [Fact]
    public void TheAlignmentCentresOfEveryVersion_AreWhereTheStandardsRulePutsThem()
    {
        // The reader works the centres out by rule; the encoder lists them. A symbol of each
        // version reads back only if the two agree on which modules are not data.
        for (var version = AdminQrCode.MinVersion; version <= AdminQrCode.MaxVersion; version++)
        {
            var data = new byte[AdminQrCode.ByteCapacity(version)];
            Array.Fill(data, (byte)'7');

            var reading = QrTestReader.Read(QrTestReader.ModulesOf(AdminQrCode.Encode(data)));

            Assert.Equal(version, reading.Version);
            Assert.Equal(data, reading.Data);
            var centres = QrTestReader.AlignmentCentres(version);
            Assert.Equal(version == 1 ? 0 : (version / 7) + 2, centres.Length);
            if (version > 1)
            {
                Assert.Equal(6, centres[0]);
                Assert.Equal(AdminQrCode.SizeOf(version) - 7, centres[^1]);
            }
        }
    }

    [Fact]
    public void EveryLengthUpToVersion15_AndTheShortestAndLongestOfEveryLaterVersion_ReadBack()
    {
        var random = new Random(20261126);
        var lengths = Enumerable.Range(0, AdminQrCode.ByteCapacity(15) + 1)
            .Concat(Enumerable.Range(16, 25).SelectMany(v => new[] { AdminQrCode.ByteCapacity(v - 1) + 1, AdminQrCode.ByteCapacity(v) }));
        var versionsSeen = new HashSet<int>();
        var masksSeen = new HashSet<int>();

        foreach (var length in lengths)
        {
            var data = new byte[length];
            random.NextBytes(data);

            var code = AdminQrCode.Encode(data);
            var reading = QrTestReader.Read(QrTestReader.ModulesOf(code));

            Assert.Equal(AdminQrCode.SmallestVersionFor(length), code.Version);
            Assert.Equal(code.Version, reading.Version);
            Assert.Equal(code.Mask, reading.Mask);
            Assert.True(data.AsSpan().SequenceEqual(reading.Data), $"{length} bytes at version {code.Version} read back wrongly");
            Assert.Equal(AdminQrCode.TotalCodewordCount(code.Version), reading.AllCodewords.Length);
            Assert.Equal(AdminQrCode.DataCodewordCount(code.Version), reading.DataCodewords.Length);
            versionsSeen.Add(code.Version);
            masksSeen.Add(code.Mask);
        }

        Assert.Equal(Enumerable.Range(1, 40), versionsSeen.Order());
        // Scoring picks different masks for different data: it is not stuck on one.
        Assert.True(masksSeen.Count >= 6, $"only masks {string.Join(',', masksSeen.Order())} were ever chosen");
    }

    [Fact]
    public void EachOfTheEightMasks_GivesAReadableSymbol_AndAskingForNoneGivesOneOfThem()
    {
        var bytes = Encoding.UTF8.GetBytes(KeyUriExample);
        var data = AdminQrCode.DataCodewords(bytes, 5);
        var chosen = AdminQrCode.FromDataCodewords(5, data);
        var symbols = Enumerable.Range(0, 8).Select(mask => AdminQrCode.FromDataCodewords(5, data, mask)).ToList();

        for (var mask = 0; mask < 8; mask++)
        {
            var reading = QrTestReader.Read(QrTestReader.ModulesOf(symbols[mask]));
            Assert.Equal(mask, symbols[mask].Mask);
            Assert.Equal(mask, reading.Mask);
            Assert.Equal(bytes, reading.Data);
        }

        Assert.Equal(
            QrTestReader.RowsOf(QrTestReader.ModulesOf(symbols[chosen.Mask])),
            QrTestReader.RowsOf(QrTestReader.ModulesOf(chosen)));
        Assert.Equal(8, symbols.Select(s => string.Concat(QrTestReader.RowsOf(QrTestReader.ModulesOf(s)))).Distinct().Count());

        Assert.Throws<ArgumentOutOfRangeException>(() => AdminQrCode.FromDataCodewords(5, data, 8));
        Assert.Throws<ArgumentException>(() => AdminQrCode.FromDataCodewords(5, data.AsSpan(1)));
        Assert.Throws<ArgumentException>(() => AdminQrCode.DataCodewords(new byte[85], 5));
    }

    [Fact]
    public void TheSvgPath_DrawsExactlyTheDarkModules_InsideAQuietZoneOfFourModules()
    {
        Assert.Equal(4, AdminQrCode.QuietZone);
        foreach (var text in new[] { "A", KeyUriExample, TwoOtpAuthAddresses })
        {
            var code = AdminQrCode.Encode(text);
            var path = code.SvgPathData();

            Assert.Equal(code.Size + 8, code.SvgSize);
            Assert.Matches(@"\A(M\d+ \d+h\d+v1h-\d+z)+\z", path);
            // FromSvgPath refuses anything drawn into the quiet zone or drawn twice.
            Assert.Equal(
                QrTestReader.RowsOf(QrTestReader.ModulesOf(code)),
                QrTestReader.RowsOf(QrTestReader.FromSvgPath(path, code.SvgSize, AdminQrCode.QuietZone)));
        }
    }

    [Fact]
    public void Text_IsEncodedAsUtf8Bytes()
    {
        const string text = "Dealoware:jörg@example.test";

        var reading = QrTestReader.Read(QrTestReader.ModulesOf(AdminQrCode.Encode(text)));

        Assert.Equal(Encoding.UTF8.GetBytes(text), reading.Data);
        Assert.Throws<ArgumentNullException>(() => AdminQrCode.Encode((string)null!));
    }
}

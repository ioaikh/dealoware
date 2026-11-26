/**
 * Reads a QR Code symbol back from its modules, for the browser tests of the
 * authenticator set-up page: what the page has painted must come back as the
 * otpauth address of the key it shows.
 *
 * It is written from the standard (ISO/IEC 18004), not from the encoder in
 * the app, and in another language: it has its own tables, checks both
 * copies of the format information and of the version information against
 * their check bits, lifts the mask, takes the blocks apart again and holds
 * every block against its Reed-Solomon check before it believes a single
 * byte. It does not find a symbol in a picture: it is handed the grid.
 *
 * It reads what the app makes: error correction level M, one byte-mode
 * segment. Anything else, and any inconsistency, throws with the reason.
 *
 * No package is needed, so this file imports nothing.
 */

export type QrReading = {
  /** 1 to 40. */
  version: number;
  /** Mask pattern, 0 to 7. */
  mask: number;
  /** The bytes of the one segment. */
  bytes: number[];
  /** The bytes as UTF-8 text. */
  text: string;
};

// Codewords in the whole symbol, by version (ISO/IEC 18004 table 1).
const TOTAL_CODEWORDS = [
  26, 44, 70, 100, 134, 172, 196, 242, 292, 346, 404, 466, 532, 581, 655, 733, 815, 901, 991, 1085,
  1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185, 2323, 2465, 2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706,
];

// At level M, by version (ISO/IEC 18004 table 9): error correction codewords in each block, and the number of blocks.
const ECC_PER_BLOCK_M = [
  10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26,
  26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
];
const BLOCKS_M = [
  1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16,
  17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49,
];

// Row and column of the alignment pattern centres, versions 2 to 40 (ISO/IEC 18004 annex E).
const ALIGNMENT: Record<number, number[]> = {
  2: [6, 18], 3: [6, 22], 4: [6, 26], 5: [6, 30], 6: [6, 34],
  7: [6, 22, 38], 8: [6, 24, 42], 9: [6, 26, 46], 10: [6, 28, 50], 11: [6, 30, 54], 12: [6, 32, 58], 13: [6, 34, 62],
  14: [6, 26, 46, 66], 15: [6, 26, 48, 70], 16: [6, 26, 50, 74], 17: [6, 30, 54, 78], 18: [6, 30, 56, 82],
  19: [6, 30, 58, 86], 20: [6, 34, 62, 90],
  21: [6, 28, 50, 72, 94], 22: [6, 26, 50, 74, 98], 23: [6, 30, 54, 78, 102], 24: [6, 28, 54, 80, 106],
  25: [6, 32, 58, 84, 110], 26: [6, 30, 58, 86, 114], 27: [6, 34, 62, 90, 118],
  28: [6, 26, 50, 74, 98, 122], 29: [6, 30, 54, 78, 102, 126], 30: [6, 26, 52, 78, 104, 130],
  31: [6, 30, 56, 82, 108, 134], 32: [6, 34, 60, 86, 112, 138], 33: [6, 30, 58, 86, 114, 142], 34: [6, 34, 62, 90, 118, 146],
  35: [6, 30, 54, 78, 102, 126, 150], 36: [6, 24, 50, 76, 102, 128, 154], 37: [6, 28, 54, 80, 106, 132, 158],
  38: [6, 32, 58, 84, 110, 136, 162], 39: [6, 26, 54, 82, 110, 138, 166], 40: [6, 30, 58, 86, 114, 142, 170],
};

/** One finder pattern, row by row: 1 is dark. */
const FINDER = ['1111111', '1000001', '1011101', '1011101', '1011101', '1000001', '1111111'];

function fail(reason: string): never {
  throw new Error(`not a readable QR Code: ${reason}`);
}

/** The mask conditions of the standard, with i the row and j the column. */
function masked(mask: number, i: number, j: number): boolean {
  switch (mask) {
    case 0: return (i + j) % 2 === 0;
    case 1: return i % 2 === 0;
    case 2: return j % 3 === 0;
    case 3: return (i + j) % 3 === 0;
    case 4: return (Math.floor(i / 2) + Math.floor(j / 3)) % 2 === 0;
    case 5: return ((i * j) % 2) + ((i * j) % 3) === 0;
    case 6: return (((i * j) % 2) + ((i * j) % 3)) % 2 === 0;
    case 7: return (((i + j) % 2) + ((i * j) % 3)) % 2 === 0;
    default: return fail(`mask ${mask}`);
  }
}

/** What is left when the bits are divided, as a polynomial over GF(2), by the generator. Zero for a valid codeword. */
function remainderOf(value: number, bits: number, generator: number, generatorBits: number): number {
  let rest = value;
  for (let bit = bits - 1; bit >= generatorBits - 1; bit -= 1) {
    if ((rest >> bit) & 1) rest ^= generator << (bit - (generatorBits - 1));
  }
  return rest;
}

export function readQr(modules: boolean[][]): QrReading {
  const size = modules.length;
  if (size < 21 || (size - 17) % 4 !== 0) fail(`${size} modules on a side`);
  for (const row of modules) if (row.length !== size) fail('not square');
  const version = (size - 17) / 4;
  if (version > 40) fail(`version ${version}`);
  const dark = (row: number, column: number): number => (modules[row][column] ? 1 : 0);

  // The three finder patterns, each with its light separator, and no fourth in the last corner.
  const corners: Array<[number, number]> = [[0, 0], [0, size - 7], [size - 7, 0]];
  for (const [top, left] of corners) {
    for (let r = -1; r <= 7; r += 1) {
      for (let c = -1; c <= 7; c += 1) {
        const row = top + r;
        const column = left + c;
        if (row < 0 || row >= size || column < 0 || column >= size) continue;
        const inside = r >= 0 && r < 7 && c >= 0 && c < 7;
        const expected = inside ? Number(FINDER[r][c]) : 0;
        if (dark(row, column) !== expected) fail(`finder pattern at ${top},${left} is broken at ${row},${column}`);
      }
    }
  }

  // The timing patterns between them: dark and light by turns, starting dark.
  for (let i = 8; i < size - 8; i += 1) {
    const expected = i % 2 === 0 ? 1 : 0;
    if (dark(6, i) !== expected || dark(i, 6) !== expected) fail(`timing pattern is broken at ${i}`);
  }
  if (dark(size - 8, 8) !== 1) fail('the dark module is light');

  // Format information: 15 bits, most significant first, kept twice.
  const firstCopy: Array<[number, number]> = [
    [8, 0], [8, 1], [8, 2], [8, 3], [8, 4], [8, 5], [8, 7], [8, 8],
    [7, 8], [5, 8], [4, 8], [3, 8], [2, 8], [1, 8], [0, 8],
  ];
  const secondCopy: Array<[number, number]> = [];
  for (let i = 1; i <= 7; i += 1) secondCopy.push([size - i, 8]);
  for (let i = 8; i >= 1; i -= 1) secondCopy.push([8, size - i]);
  const bitsAt = (places: Array<[number, number]>): number =>
    places.reduce((value, [row, column]) => (value << 1) | dark(row, column), 0);
  const format = bitsAt(firstCopy);
  if (bitsAt(secondCopy) !== format) fail('the two copies of the format information differ');
  const unmasked = format ^ 0b101010000010010;
  if (remainderOf(unmasked, 15, 0b10100110111, 11) !== 0) fail('the format information fails its check');
  // Two bits for the level (M is written 00), three for the mask.
  if (unmasked >> 13 !== 0b00) fail(`error correction level bits ${(unmasked >> 13).toString(2)}, not level M`);
  const mask = (unmasked >> 10) & 0b111;

  // Version information, from version 7: 18 bits, kept twice, mirrored.
  if (version >= 7) {
    let bottomLeft = 0;
    let topRight = 0;
    for (let i = 17; i >= 0; i -= 1) {
      bottomLeft = (bottomLeft << 1) | dark(size - 11 + (i % 3), Math.floor(i / 3));
      topRight = (topRight << 1) | dark(Math.floor(i / 3), size - 11 + (i % 3));
    }
    if (bottomLeft !== topRight) fail('the two copies of the version information differ');
    if (remainderOf(bottomLeft, 18, 0b1111100100101, 13) !== 0) fail('the version information fails its check');
    if (bottomLeft >> 12 !== version) fail(`the symbol says version ${bottomLeft >> 12} but has the size of version ${version}`);
  }

  // Which modules are not data.
  const reserved: boolean[][] = modules.map((row) => row.map(() => false));
  const reserve = (top: number, left: number, height: number, width: number): void => {
    for (let row = top; row < top + height; row += 1) {
      for (let column = left; column < left + width; column += 1) reserved[row][column] = true;
    }
  };
  reserve(0, 0, 9, 9);
  reserve(0, size - 8, 9, 8);
  reserve(size - 8, 0, 8, 9);
  reserve(6, 0, 1, size);
  reserve(0, 6, size, 1);
  const centres = ALIGNMENT[version] ?? [];
  for (const row of centres) {
    for (const column of centres) {
      const onAFinder = (row === 6 && column === 6) || (row === 6 && column === size - 7) || (row === size - 7 && column === 6);
      if (onAFinder) continue;
      // An alignment pattern: dark, light, dark from the outside in.
      for (let r = -2; r <= 2; r += 1) {
        for (let c = -2; c <= 2; c += 1) {
          const expected = Math.max(Math.abs(r), Math.abs(c)) === 1 ? 0 : 1;
          if (dark(row + r, column + c) !== expected) fail(`alignment pattern at ${row},${column} is broken`);
        }
      }
      reserve(row - 2, column - 2, 5, 5);
    }
  }
  if (version >= 7) {
    reserve(size - 11, 0, 3, 6);
    reserve(0, size - 11, 6, 3);
  }

  // The data: columns two modules wide from the right, up and down by turns, right module first.
  const stream: number[] = [];
  let upward = true;
  for (let right = size - 1; right > 0; right -= 2) {
    if (right === 6) right = 5;
    for (let n = 0; n < size; n += 1) {
      const row = upward ? size - 1 - n : n;
      for (const column of [right, right - 1]) {
        if (!reserved[row][column]) stream.push(dark(row, column) ^ (masked(mask, row, column) ? 1 : 0));
      }
    }
    upward = !upward;
  }

  const total = TOTAL_CODEWORDS[version - 1];
  const spare = stream.length - total * 8;
  if (spare < 0 || spare > 7) fail(`${stream.length} data modules for ${total} codewords`);
  if (stream.slice(total * 8).some((bit) => bit !== 0)) fail('the modules after the last codeword are not empty');
  const codewords: number[] = [];
  for (let i = 0; i < total; i += 1) {
    let value = 0;
    for (let bit = 0; bit < 8; bit += 1) value = (value << 1) | stream[i * 8 + bit];
    codewords.push(value);
  }

  // The blocks: woven together codeword by codeword, the shorter ones first.
  const blockCount = BLOCKS_M[version - 1];
  const eccLength = ECC_PER_BLOCK_M[version - 1];
  const dataTotal = total - blockCount * eccLength;
  const shortLength = Math.floor(dataTotal / blockCount);
  const longBlocks = dataTotal % blockCount;
  const blocks: number[][] = [];
  for (let b = 0; b < blockCount; b += 1) blocks.push([]);
  const dataLength = (b: number): number => shortLength + (b >= blockCount - longBlocks ? 1 : 0);
  let at = 0;
  for (let i = 0; i <= shortLength; i += 1) {
    for (let b = 0; b < blockCount; b += 1) {
      if (i < dataLength(b)) blocks[b].push(codewords[at++]);
    }
  }
  for (let i = 0; i < eccLength; i += 1) {
    for (let b = 0; b < blockCount; b += 1) blocks[b].push(codewords[at++]);
  }
  if (at !== total) fail('the blocks do not use up the codewords');

  // Each block is a Reed-Solomon codeword over GF(256): every syndrome is zero.
  const exp: number[] = [];
  const log: number[] = new Array(256).fill(0);
  let x = 1;
  for (let i = 0; i < 255; i += 1) {
    exp[i] = x;
    log[x] = i;
    x <<= 1;
    if (x & 0x100) x ^= 0x11d;
  }
  const times = (a: number, b: number): number => (a === 0 || b === 0 ? 0 : exp[(log[a] + log[b]) % 255]);
  blocks.forEach((block, b) => {
    for (let root = 0; root < eccLength; root += 1) {
      let syndrome = 0;
      for (const codeword of block) syndrome = times(syndrome, exp[root]) ^ codeword;
      if (syndrome !== 0) fail(`block ${b + 1} of ${blockCount} fails its error correction check`);
    }
  });

  // The message: byte mode, a count, the bytes, then the terminator and padding.
  const data: number[] = [];
  blocks.forEach((block, b) => data.push(...block.slice(0, dataLength(b))));
  let position = 0;
  const take = (count: number): number => {
    let value = 0;
    for (let i = 0; i < count; i += 1, position += 1) {
      value = (value << 1) | ((data[position >> 3] >> (7 - (position & 7))) & 1);
    }
    return value;
  };
  const mode = take(4);
  if (mode !== 0b0100) fail(`mode ${mode.toString(2).padStart(4, '0')}, not byte mode`);
  const count = take(version <= 9 ? 8 : 16);
  if (position + count * 8 > data.length * 8) fail(`a count of ${count} does not fit`);
  const bytes: number[] = [];
  for (let i = 0; i < count; i += 1) bytes.push(take(8));
  const capacity = data.length * 8;
  if (take(Math.min(4, capacity - position)) !== 0) fail('no terminator after the data');
  while (position % 8 !== 0) if (take(1) !== 0) fail('the padding bits are not zero');
  for (let pad = 0; position < capacity; pad += 1) {
    if (take(8) !== (pad % 2 === 0 ? 0xec : 0x11)) fail('the pad codewords are not the standard ones');
  }

  return { version, mask, bytes, text: new TextDecoder('utf-8', { fatal: true }).decode(new Uint8Array(bytes)) };
}

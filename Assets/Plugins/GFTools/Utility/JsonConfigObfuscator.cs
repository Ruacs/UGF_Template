using System;
using System.Security.Cryptography;

namespace Lokas
{
    public static class JsonConfigObfuscator
    {
        private static readonly byte[] MagicHeader = { 0x4E, 0x30, 0x30, 0x3F };
        private static readonly byte[] MagicTail = { 0x62, 0x6F, 0x6C, 0x79 };
        private const int HeaderLength = 8;
        private const int TailLength = 4;
        private const byte MarkerV1 = 0x01;
        private const byte MarkerV2 = 0x02;
        private const int MaxPaddingLength = 255;
        private const uint MaskSeed = 0x6A09E667;

        public static bool IsObfuscated(byte[] data)
        {
            return data != null
                && data.Length >= HeaderLength + TailLength
                && StartsWith(data, MagicHeader)
                && EndsWith(data, MagicTail);
        }

        public static byte[] Obfuscate(byte[] plainData)
        {
            if (plainData == null)
                throw new ArgumentNullException(nameof(plainData));

            int swapLen = GetSwapLength(plainData.Length);
            int padLen = GetRandomByte();
            byte[] result = new byte[HeaderLength + plainData.Length + padLen + TailLength];

            Buffer.BlockCopy(MagicHeader, 0, result, 0, MagicHeader.Length);
            result[4] = (byte)((swapLen >> 8) & 0xFF);
            result[5] = (byte)(swapLen & 0xFF);
            result[6] = MarkerV2;
            result[7] = (byte)padLen;

            WriteSwappedBody(plainData, result, HeaderLength, swapLen);
            ApplyBodyMask(result, HeaderLength, plainData.Length, swapLen, padLen, result.Length);
            FillRandom(result, HeaderLength + plainData.Length, padLen);
            Buffer.BlockCopy(MagicTail, 0, result, result.Length - TailLength, TailLength);
            return result;
        }

        public static byte[] Deobfuscate(byte[] data)
        {
            if (!IsObfuscated(data))
                return data;

            int swapLen = (data[4] << 8) | data[5];
            byte marker = data[6];
            int padLen = data[7];
            int bodyLength = data.Length - HeaderLength - padLen - TailLength;
            if (bodyLength < 0 || swapLen * 2 > bodyLength)
                throw new FormatException("Invalid obfuscated json config bytes.");

            byte[] body = new byte[bodyLength];
            Buffer.BlockCopy(data, HeaderLength, body, 0, bodyLength);
            if (marker >= MarkerV2)
            {
                ApplyBodyMask(body, 0, bodyLength, swapLen, padLen, data.Length);
            }

            byte[] plainData = new byte[bodyLength];
            if (swapLen <= 0)
            {
                Buffer.BlockCopy(body, 0, plainData, 0, bodyLength);
                return plainData;
            }

            int middleLength = bodyLength - swapLen * 2;
            Buffer.BlockCopy(body, bodyLength - swapLen, plainData, 0, swapLen);
            Buffer.BlockCopy(body, swapLen, plainData, swapLen, middleLength);
            Buffer.BlockCopy(body, 0, plainData, bodyLength - swapLen, swapLen);
            return plainData;
        }

        private static int GetSwapLength(int dataLength)
        {
            int maxSwapLen = Math.Min(ushort.MaxValue, dataLength / 2);
            if (maxSwapLen <= 0)
                return 0;

            return 1 + (GetRandomUInt16() % maxSwapLen);
        }

        private static void WriteSwappedBody(byte[] plainData, byte[] result, int offset, int swapLen)
        {
            if (swapLen <= 0)
            {
                Buffer.BlockCopy(plainData, 0, result, offset, plainData.Length);
                return;
            }

            int middleLength = plainData.Length - swapLen * 2;
            Buffer.BlockCopy(plainData, plainData.Length - swapLen, result, offset, swapLen);
            Buffer.BlockCopy(plainData, swapLen, result, offset + swapLen, middleLength);
            Buffer.BlockCopy(plainData, 0, result, offset + swapLen + middleLength, swapLen);
        }

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            for (int i = 0; i < prefix.Length; i++)
            {
                if (data[i] != prefix[i])
                    return false;
            }

            return true;
        }

        private static bool EndsWith(byte[] data, byte[] suffix)
        {
            int offset = data.Length - suffix.Length;
            for (int i = 0; i < suffix.Length; i++)
            {
                if (data[offset + i] != suffix[i])
                    return false;
            }

            return true;
        }

        private static void ApplyBodyMask(byte[] buffer, int offset, int count, int swapLen, int padLen, int totalLength)
        {
            uint state = MaskSeed
                ^ ((uint)swapLen << 16)
                ^ (uint)(padLen << 8)
                ^ (uint)totalLength;

            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] ^= NextMaskByte(ref state);
            }
        }

        private static byte NextMaskByte(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (byte)(state & 0xFF);
        }

        private static int GetRandomByte()
        {
            byte[] buffer = new byte[1];
            FillRandom(buffer, 0, buffer.Length);
            return buffer[0] % (MaxPaddingLength + 1);
        }

        private static int GetRandomUInt16()
        {
            byte[] buffer = new byte[2];
            FillRandom(buffer, 0, buffer.Length);
            return (buffer[0] << 8) | buffer[1];
        }

        private static void FillRandom(byte[] buffer, int offset, int count)
        {
            if (count <= 0)
                return;

            byte[] randomBytes = new byte[count];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            Buffer.BlockCopy(randomBytes, 0, buffer, offset, count);
        }
    }
}

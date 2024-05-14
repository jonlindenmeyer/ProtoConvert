using System.IO.Compression;
using System.Text.Json;
using ProtoBuf;

namespace ProtoConvertTests.Encoding
{
    public static class ProtobufDecoder
    {
        private static readonly JsonSerializerOptions Options = new ()
        {
            WriteIndented = true
        };

        public static string Decode(string input, Type type)
        {
            if (input.Length % 2 == 1)
            {
                return "invalid input";
            }

            try
            {
                var byteCount = input.Length / 2;

                var bytes = new byte[byteCount];

                for (var i = 0; i < byteCount; i++)
                {
                    bytes[i] = (byte)((GetHexValue(input[i * 2]) << 4) + GetHexValue(input[(i * 2) + 1]));
                }

                using var stream = new MemoryStream(bytes);

                Stream readStream = stream;

                if (bytes[0] == 0x1f && bytes[1] == 0x8b)
                {
                    readStream = new GZipStream(stream, CompressionMode.Decompress);
                }

                var obj = Serializer.Deserialize(type, readStream);

                return JsonSerializer.Serialize(obj, type, Options);
            }
            catch (Exception ex)
            {
                return $"failed: {ex}";
            }
        }

        private static int GetHexValue(char ch)
        {
            return ch - (ch < 58 ? 48 : (ch < 97 ? 55 : 87));
        }
    }
}

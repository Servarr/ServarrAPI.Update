using System.Data;
using System.IO;
using System.IO.Compression;
using Dapper;

namespace ServarrAPI.Datastore.Converters
{
    public class GzipConverter : SqlMapper.TypeHandler<byte[]>
    {
        public override void SetValue(IDbDataParameter parameter, byte[] value)
        {
            parameter.Value = Compress(value);
        }

        public override byte[] Parse(object value)
        {
            return Decompress((byte[])value);
        }

        private static byte[] Compress(byte[] data)
        {
            using var compressedStream = new MemoryStream();
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Compress);
            zipStream.Write(data, 0, data.Length);
            zipStream.Close();

            return compressedStream.ToArray();
        }

        private static byte[] Decompress(byte[] data)
        {
            using var compressedStream = new MemoryStream(data);
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();

            zipStream.CopyTo(resultStream);

            return resultStream.ToArray();
        }
    }
}

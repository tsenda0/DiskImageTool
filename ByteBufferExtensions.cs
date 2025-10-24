namespace DiskImageTool
{
    public static class ByteBufferExtensions
    {
        public static ushort GetUshort(this IList<byte> buffer, int offset)
        {
            // リトルエンディアンとして2バイトを結合
            return (ushort)(buffer[offset++] | (buffer[offset++] << 8));
        }

        public static uint GetUint(this IList<byte> buffer, int offset)
        {
            // リトルエンディアンとして2バイトを結合
            return (uint)(buffer[offset++]
                | buffer[offset++] << 8
                | buffer[offset++] << 16
                | buffer[offset++] << 24);
        }
    }
}

using System.Buffers;
using System.Buffers.Text;

namespace Voltaic.Serialization.Utf8
{
    public static partial class Utf8Writer
    {
        public static bool TryWrite(ref ResizableMemory<byte> writer, float value, StandardFormat standardFormat)
        {
            var data = writer.RequestSpan(14); // -3.402823E+038
            if (!Utf8Formatter.TryFormat(value, data, out int bytesWritten, standardFormat))
                return false;
            writer.Advance(bytesWritten);
            return true;
        }

        public static bool TryWrite(ref ResizableMemory<byte> writer, double value, StandardFormat standardFormat)
        {
            var data = writer.RequestSpan(22); // -1.79769313486232E+308
            if (!Utf8Formatter.TryFormat(value, data, out int bytesWritten, standardFormat))
                return false;
            writer.Advance(bytesWritten);
            return true;
        }

        public static bool TryWrite(ref ResizableMemory<byte> writer, decimal value, StandardFormat standardFormat)
        {
            var data = writer.RequestSpan(30); // -79228162514264337593543950336
            if (!Utf8Formatter.TryFormat(value, data, out int bytesWritten, standardFormat))
                return false;
            writer.Advance(bytesWritten);
            return true;
        }
    }
}

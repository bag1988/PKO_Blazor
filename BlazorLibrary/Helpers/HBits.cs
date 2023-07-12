namespace BlazorLibrary.Helpers
{
    public class HBits
    {
        public bool CHECK_BIT(int var, int pos) => ((var & 1 << pos) == 1 << pos);
        public int SET_BIT(int var, int pos) => ((var) |= (1 << (pos)));
        public int RESET_BIT(int var, int pos) => ((var) &= ~(1 << (pos)));
        public int HIWORD(int l) => ((l) >> 16) & 0xffff;
        public int LOWORD(int l) => l & 0xffff;
        public int MAKELONG(int a, int b) => ((a & 0xffff) | (b & 0xffff) << 16);
        public byte LOBYTE(int w) => (byte)(w & 0xff);
        public int BIT_INVERT(int x, int y) => (x & y) > 0 ? (x & ~y) : (x | y);
        public ushort MAKEWORD(byte a, byte b) => (ushort)((a & 0xff) | ((ushort)(b & 0xff)) << 8);
    }
}

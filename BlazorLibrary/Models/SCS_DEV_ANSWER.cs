namespace BlazorLibrary.Models
{
    public class SCS_DEV_ANSWER
    {
        public byte InfoType { get; set; }
        public byte ChannelNumber { get; set; }
        public byte[] Param { get; set; } = new byte[8];
        public uint LineNumber { get; set; }
        public uint SerNo { get; set; }

        public SCS_DEV_ANSWER(byte[] State)
        {
            if (State.Length > 0)
                InfoType = State[0];
            if (State.Length > 1)
                ChannelNumber = State[1];
            if (State.Length > 2)
                State.Skip(2).Take(8).ToArray().CopyTo(Param, 0);
            if (State.Length > 11)
                LineNumber = BitConverter.ToUInt32(State, 9);
            if (State.Length > 15)
                SerNo = BitConverter.ToUInt32(State, 13);
        }
    }


}

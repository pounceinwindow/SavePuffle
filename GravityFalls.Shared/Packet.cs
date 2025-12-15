using System.Text.Json;

namespace GravityFalls.Shared
{
    public class Packet
    {
        public static byte[] Serialize(OpCode code, object data)
        {
            string json = JsonSerializer.Serialize(data);
            byte[] encryptedBody = CryptoHelper.Encrypt(json);

            var packet = new List<byte>();
            
            // Header: Length (4 bytes)
            packet.AddRange(BitConverter.GetBytes(encryptedBody.Length + 1));
            
            // Header: OpCode (1 byte)
            packet.Add((byte)code);
            
            // Body: Encrypted JSON
            packet.AddRange(encryptedBody);

            return packet.ToArray();
        }
    }
}
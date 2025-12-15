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
            
            packet.AddRange(BitConverter.GetBytes(encryptedBody.Length + 1));
            
            packet.Add((byte)code);
            
            packet.AddRange(encryptedBody);

            return packet.ToArray();
        }
    }
}
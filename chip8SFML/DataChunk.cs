using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace chip8SFML
{
    public class DataChunk
    {
        private const string CHUNK_ID = "data";

        public string ChunkId { get; private set; }
        public UInt32 ChunkSize { get; set; }
        public short[] WaveData { get; private set; }

        public DataChunk()
        {
            ChunkId = CHUNK_ID;
            ChunkSize = 0; // This changes when data is added
        }

        public UInt32 Length()
        {
            return (UInt32)GetBytes().Length;
        }

        public byte[] GetBytes()
        {
            List<Byte> chunkBytes = new List<byte>();

            chunkBytes.AddRange(Encoding.ASCII.GetBytes(ChunkId));
            chunkBytes.AddRange(BitConverter.GetBytes(ChunkSize));
            byte[] buffer = new byte[WaveData.Length * 2];
            Buffer.BlockCopy(WaveData, 0, buffer, 0, buffer.Length);
            chunkBytes.AddRange(buffer.ToList());

            return chunkBytes.ToArray();
        }

        public void AddSampleData(short[] leftBuffer, short[] rightBuffer)
        {
            WaveData = new short[leftBuffer.Length + rightBuffer.Length];
            int bufferOffset = 0;
            for (int i = 0; i < WaveData.Length; i += 2)
            {
                WaveData[i] = leftBuffer[bufferOffset];
                WaveData[i + 1] = rightBuffer[bufferOffset];
                bufferOffset++;
            }

            ChunkSize = (UInt32)WaveData.Length * 2;
        }
    }
}

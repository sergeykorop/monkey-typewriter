using System;

namespace MonkeyTypewriter
{
	class MonkeyTypewriter
	{
		private static System.Collections.Generic.IEnumerable<byte> GetSeries(Random rng, uint size)
		{
			uint count = 0;

			for(uint i = 0; i < (size + 3)/4; i++)
			{
				int datum = rng.Next();

				for(uint j = 0; j < 4; j++)
				{
					if(count == size)
						yield break ;

					yield return (byte)(datum & 0xFF);

					datum >>= 8;

					count++;
				}
				
			}
		}

		private static void DumpSeries(System.Collections.Generic.IEnumerator<byte> seriesIter, uint pos, uint size)
		{
			byte[] lineBuf = new byte[16];

			uint bytesCount = 0;

			bool endOfStream = false;

			uint currentPos = pos & ~0x0FU;

			for(uint linePos = 0; ; linePos = (linePos + 1) & 0x0000000F)
			{
				if(currentPos < pos)
				{
					if(linePos == 0)
						Console.Write("{0:X8}", pos);

					Console.Write("   ");

					lineBuf[linePos] = 0x20;

					currentPos++;
				}
				else if(bytesCount < size && seriesIter.MoveNext())
				{
					byte b = seriesIter.Current;

					if(linePos == 0)
						Console.Write("{0:X8}", currentPos);

					Console.Write(" {0:X2}", b);

					if(b >= 32 && b < 127)
						lineBuf[linePos] = b;
					else
						lineBuf[linePos] = 0x2E;

					currentPos++;
					bytesCount++;
				}
				else
				{
					Console.Write("   ");
					lineBuf[linePos] = 0x20;

					endOfStream = true;
				}

				if(linePos == 15)
				{
					Console.Write(" {0}\n", System.Text.Encoding.ASCII.GetString(lineBuf));

					if(endOfStream)
						break;
				}
			}
		}

		public static void Main(string[] args)
		{
			Random rng = new Random();

			int [] initialState = 
			{
				0x68543C38,
				0x5F9027AD,
				0x09F7E43D,
				0x0BBBDE32,
				0x08E596D8,
				0x1DB07915,
				0x4D6E4E6F,
				0x789CADC3,
				0x2688E25C,
				0x478C8F5F,
				0x39F69FBF,
				0x4DCD01F6,
				0x16C7A626,
				0x3BEBB85E,
				0x12095552,
				0x6FFCA587,
				0x066F3FCF,
				0x44BF7CA0,
				0x4AF02D8F,
				0x112C7E55,
				0x7682FAEE,
				0x298604BF,
				0x5CE94965,
				0x03B75F79,
				0x55636555,
				0x20089EB1,
				0x299473B5,
				0x7A108284,
				0x0D3A1DB2,
				0x47EDD085,
				0x735C6637,
				0x4E213D7B,
				0x30EAE5DC,
				0x1AF62A0F,
				0x6BCD1D8A,
				0x161674EB,
				0x54F046EA,
				0x56F4A900,
				0x00D99860,
				0x56B34220,
				0x17D901F4,
				0x075E8665,
				0x77B0EBA7,
				0x1B01A6D5,
				0x61745424,
				0x0617733F,
				0x2AF28824,
				0x38914069,
				0x45141400,
				0x358B691D,
				0x7F9ABA75,
				0x58CB328A,
				0x05E1071D,
				0x1A642830,
				0x6A8C766C,
			};
			
			rng.SetState(initialState);

			const uint startingChunkSize  = 100;
			const uint payloadOffset      = 1000;
			const uint payloadSize        = 250;
			const uint payloadPaddingSize = 50;
			const uint skippedChunkSize   = payloadOffset - payloadPaddingSize - startingChunkSize;

			var series = GetSeries(rng, payloadOffset + payloadSize);

			var seriesIter = series.GetEnumerator();

			DumpSeries(seriesIter, 0, startingChunkSize);

			Console.WriteLine("\nSkipping {0} bytes...\n", skippedChunkSize);

			for(int i = 0; i < skippedChunkSize; i++)
				seriesIter.MoveNext();

			DumpSeries(seriesIter, startingChunkSize + skippedChunkSize, payloadPaddingSize + payloadSize);
		}
	}
}
using System;

namespace MonkeyTypewriter
{
	class Prepare
	{
		public static void Main(string[] args)
		{
			if(args.Length < 2)
			{
				Console.WriteLine("Usage: prepare <message> <offset>");
				return;
			}

			string message = args[0];

			int offset = int.Parse(args[1]);

			if(offset % 4 != 0)
				Console.WriteLine("Warning: offset is truncated to the multiple of 4");


			byte[] msgBytes = System.Text.Encoding.ASCII.GetBytes(message);

			Random rng = new Random(offset);

			int[] finalState = rng.GetState();			

			Buffer.BlockCopy(msgBytes, 0, finalState, 0, msgBytes.Length);

			ulong effectiveOffset = (ulong) (offset / 4 + finalState.Length);

			int [,] transitionMatrix = RandomExt.MatPow(RandomExt.BackwardStep(), effectiveOffset);

			int[] seed = RandomExt.MatMul(transitionMatrix, finalState);

			for(int i = 0; i < seed.Length; i++)
				Console.WriteLine("0x{0:X8},", seed[i]);
		}
	}
}
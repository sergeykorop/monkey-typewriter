using System;

namespace MonkeyTypewriter
{
	class Explore
	{
		static void CompareStates(params int[][] states)
		{
			for(int i = 0; i < states[0].Length; i++)
			{
				Console.Write("{0,2}", i);
				for(int j = 0; j < states.Length; j++)
					Console.Write(", {0:X8}", states[j][i]);
				Console.WriteLine();
			}
		}

		public static void Main(string[] args)
		{
			Random rng = new Random(42);

			int[][] states = new int[5][];

			for(int i = 0; i < states.Length - 1; i++)
			{
				states[i] = rng.GetState();

				Console.WriteLine("{0:X}", rng.Next());
			}

			states[states.Length - 1] = rng.GetState();

			Console.WriteLine();

			CompareStates(states);
		}
	}
}
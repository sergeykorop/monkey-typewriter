using System;

namespace MonkeyTypewriter
{
	public static class RandomExt
	{
		private const int MBIG = Int32.MaxValue;

        private static readonly System.Reflection.FieldInfo seedArrayField;
        private static readonly System.Reflection.FieldInfo inextField;
        private static readonly System.Reflection.FieldInfo inextpField;

        static RandomExt()
        {
            Type rngType = typeof(Random);

            seedArrayField = rngType.GetField("SeedArray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            inextField     = rngType.GetField("inext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            inextpField    = rngType.GetField("inextp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }


        public static int[] GetState(this Random rng)
		{
			int   inext      = (int)inextField.GetValue(rng);
			int[] seedArray  = (int[])seedArrayField.GetValue(rng);

			int[] state = new int[seedArray.Length - 1];

			int upperChunkSize = seedArray.Length - (inext + 1);

			Array.Copy(seedArray, inext + 1, state, 0, upperChunkSize);
			Array.Copy(seedArray, 1, state, upperChunkSize, inext);

			return state;
		}

		public static void SetState(this Random rng, int[] state)
		{
			int[] seedArray = (int[])seedArrayField.GetValue(rng);

			state.CopyTo(seedArray, 1);

			inextField.SetValue(rng, 0);
			inextpField.SetValue(rng, 21);
		}

		public static int[,] ForwardStep()
		{
			int[,] A = new int[55, 55] ;

			for(int row = 0; row < 54; row++)
				A[row, row + 1] = 1;

			A[54,  0] = 1;
			A[54, 21] = MBIG - 1;

			return A;
		}

		public static int[,] BackwardStep()
		{
			int[,] A = new int[55, 55] ;

			A[0, 20] = 1;
			A[0, 54] = 1;

			for(int row = 1; row < 55; row++)
				A[row, row - 1] = 1;
						
			return A;
		}

		public static int[,] MatMul(int[,] A, int[,] B)
		{
			int[,] C = new int[55,55];

			for(int i = 0; i < 55; i++)
			{
				for(int j = 0; j < 55; j++)
				{
					C[i, j] = 0;
					for(int k = 0; k < 55; k++)
					{
						C[i, j] = (int)(((ulong)A[i, k] * (ulong)B[k, j] + (ulong)C[i, j]) % MBIG);
					}
				}
			}

			return C;
		}

		public static int[] MatMul(int[,] A, int[] B)
		{
			int[] C = new int[55];

			for(int i = 0; i < 55; i++)
			{
				C[i] = 0;
				for(int k = 0; k < 55; k++)
				{
					C[i] = (int)(((ulong)A[i, k] * (ulong)B[k] + (ulong)C[i]) % MBIG);
				}
			}
			
			return C;
		}
		
		public static int[,] MatPow(int[,] A, ulong n)
		{
			int[,] C = new int[55, 55];

			int [,] T = (int[,])A.Clone();

			for(int i = 0; i < 55; i++)
				C[i, i] = 1;

			for(ulong k = n ; k > 0 ; k /= 2)
			{
				if(k % 2 != 0) 
				{
					C = MatMul(C, T);
				}

				T = MatMul(T, T);
			}
			
			return C;
		}
	};
}
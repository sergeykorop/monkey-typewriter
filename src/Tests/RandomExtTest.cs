using System;
using NUnit.Framework;
using MonkeyTypewriter;

namespace MonkeyTypewriter.Tests
{
	[TestFixture]
	public class RandomExtTest
	{
		private const uint nTrials = 100;

		private int[] GetSample(Random rng)
		{
			int[] sample = new int[100];

			for(int i = 0; i < sample.Length; i++)
				sample[i] = rng.Next();

			return sample;
		}

		[Test]
		public void SaveState()
		{
			Random rng = new Random(42);

			for(uint i = 0; i < nTrials; i++)
			{
				var st = rng.GetState();

				var s1 = GetSample(rng);

				rng.SetState(st);

				var s2 = GetSample(rng);

				Assert.AreEqual(s1, s2);
			}
		}

		[Test]
		public void JumpBackward()
		{
			Random rng = new Random(42);

			for(uint i = 0; i < nTrials; i++)
			{
				var s1 = GetSample(rng);

				var st = rng.GetState();

				var tr = RandomExt.MatPow(RandomExt.BackwardStep(), (ulong)s1.Length);

				rng.SetState(RandomExt.MatMul(tr, st));

				var s2 = GetSample(rng);

				Assert.AreEqual(s1, s2);
			}
		}

		[Test]
		public void JumpForward()
		{
			Random rng = new Random(42);

			for(uint i = 0; i < nTrials; i++)
			{

				var st = rng.GetState();

				var s1 = GetSample(rng);
				var s2 = GetSample(rng);

				var tr = RandomExt.MatPow(RandomExt.ForwardStep(), (ulong)s1.Length);

				rng.SetState(RandomExt.MatMul(tr, st));

				var s3 = GetSample(rng);

				Assert.AreEqual(s2, s3);
			}
		}
	}
}

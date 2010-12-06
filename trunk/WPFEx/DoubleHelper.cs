using System;

namespace org.pescuma.wpfex
{
	internal class DoubleHelper
	{
		public static bool AreEqual(double a, double b, double precision = 0.01)
		{
			return Math.Abs(a - b) < precision;
		}
	}
}

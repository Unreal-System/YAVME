using System;

namespace V1
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Executive.Init();

			Executive.Run(new byte[] { 0, 1, 7, 7, 2, 255 });
		}
	}
}

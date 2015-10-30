using System;
using System.Linq;

namespace PanoramicData.LicenceMagic.Test
{
	[Serializable]
	public class TestLicenceDetails : LicenceDetails
	{
		private static readonly byte[] InitVector = Enumerable.Range(0, Crypto.InitVectorSize).Select(i => (byte)i).ToArray();

		public TestLicenceDetails() : base(InitVector)
		{
		}
	}
}
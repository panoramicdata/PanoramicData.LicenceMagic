using System;
using System.Linq;
using PanoramicData.LicenceMagic.Licences;

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
# PanoramicData.LicenceMagic

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/a73b06e95f2e45698c4e96ddea71be2b)](https://app.codacy.com/gh/panoramicdata/PanoramicData.LicenceMagic/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![NuGet Version](https://img.shields.io/nuget/v/PanoramicData.LicenceMagic)](https://www.nuget.org/packages/PanoramicData.LicenceMagic)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PanoramicData.LicenceMagic)](https://www.nuget.org/packages/PanoramicData.LicenceMagic)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Introduction

PanoramicData.LicenceMagic is a .NET 10 library for creating, signing, writing, reading, and validating application licence files.

Licence signatures are bound to a filename and validated against the licensed company, product, validity dates, and assembly version range.

## Installation

```shell
dotnet add package PanoramicData.LicenceMagic
```

## Usage

Define licence details with a fixed 16-byte initialization vector:

```csharp
using PanoramicData.LicenceMagic;

public sealed class ProductLicence : LicenceDetails
{
	private static readonly byte[] InitVector =
		[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];

	public ProductLicence() : base(InitVector)
	{
	}
}
```

Create and write a signed licence:

```csharp
var salt = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
var file = new FileInfo("product.lic");
var details = new ProductLicence
{
	LicensedCompany = "Example Ltd",
	LicensedProduct = "Example Product",
	StartDateUtc = DateTime.UtcNow.Date,
	EndDateUtc = DateTime.UtcNow.Date.AddYears(1),
	StartVersion = "1.0",
	EndVersion = "2.0"
};

details.Sign(file.Name, salt);
new License<ProductLicence>(details).WriteToFile(file, salt);
```

Read and validate it:

```csharp
var licence = new License<ProductLicence>(file);
var validation = licence.Validate(salt);
if (!validation.IsValid)
{
	throw new InvalidOperationException(validation.ErrorMessage);
}
```

Keep the salt and initialization vector stable and protect them as application secrets. Existing licence files depend on the values used when they were signed.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).

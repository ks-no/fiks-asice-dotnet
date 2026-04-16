# fiks-asice
[![MIT license](https://img.shields.io/badge/license-MIT-blue.svg)](//github.com/ks-no/fiks-asice-dotnet/blob/main/LICENSE)
[![Nuget](https://img.shields.io/nuget/vpre/KS.Fiks.ASiC-E.svg)](https://www.nuget.org/packages/KS.Fiks.ASiC-E)
[![GitHub issues](https://img.shields.io/github/issues-raw/ks-no/fiks-asice-dotnet.svg)](//github.com/ks-no/fiks-asice-dotnet/issues)
[![GitHub Release Date](https://img.shields.io/github/release-date/ks-no/fiks-asice-dotnet)](//github.com/ks-no/fiks-asice-dotnet/releases)

Library for working with ASiC-E packages in .Net Core based projects.
This project is created by KS and released under a MIT license.

The long term goal of this project is to be functionally compliant with the
[DIFI ASiC library for Java](//github.com/difi/asic/).

## Features

Currently implemented:
* Building ASiC-E packages with CAdES manifest descriptors
* Building ASiC-E packages with XAdES signatures
* Building ASiC-E packages with custom manifests
* Signing packages using private/public keys in PEM files or `X509Certificate2`
* Reading ASiC-E packages and exposing CAdES signatures

TODO:
* Support for specifying OASIS manifest
* Support for verifying XAdES signatures

## Installation

```
dotnet add package KS.Fiks.ASiC-E
```

Requires **.NET 8** or **.NET Standard 2.0 / 2.1**.

## Local development

```bash
git clone https://github.com/ks-no/fiks-asice-dotnet.git
cd fiks-asice-dotnet
dotnet build
dotnet test
```

## Examples

### Create ASiC-E package containing a single file

```csharp
using (var outStream = /* output stream */)
using (var fileStream = /* file to include */)
using (var asiceBuilder = AsiceBuilder.Create(outStream, MessageDigestAlgorithm.SHA256, certificateHolder))
{
    asiceBuilder.AddFile(fileStream, "document.pdf");
    var asiceArchive = asiceBuilder.Build();
    // The archive has been written to outStream
}
```

### Sign and create an ASiC-E package using PEM key files

```csharp
// Load certificate and private key from PEM files
ICertificateHolder certificateHolder = PreloadedCertificateHolder.Create(
    File.ReadAllBytes("public.pem"),
    File.ReadAllBytes("private.pem"));

using (var outStream = new FileStream("archive.asice", FileMode.Create))
using (var fileStream = File.OpenRead("document.pdf"))
using (var asiceBuilder = AsiceBuilder.Create(outStream, MessageDigestAlgorithm.SHA256, certificateHolder))
{
    asiceBuilder.AddFile(fileStream, "document.pdf");
    asiceBuilder.Build();
}
```

Alternatively, use an `X509Certificate2` from the Windows certificate store:

```csharp
var x509 = /* load X509Certificate2 with private key */;
ICertificateHolder certificateHolder = new SystemX509CertificateHolder(x509);
```

### Read data from an ASiC-E package

```csharp
IAsicReader reader = new AsiceReader();
using (var inputStream = File.OpenRead("archive.asice"))
using (var asice = reader.Read(inputStream))
{
    foreach (var entry in asice.Entries)
    {
        using (var entryStream = entry.OpenStream())
        {
            // process entryStream
        }
    }

    // Verify all digests declared in the manifest
    if (asice.DigestVerifier.Verification().AllValid)
    {
        // All digests are valid
    }
    else
    {
        // Handle verification failure
    }
}
```

## Integration testing

This library is integration-tested against the Java implementation via
[ks-no/asice-testsuite](https://github.com/ks-no/asice-testsuite), which verifies
cross-platform compatibility between the .NET and Java ASiC-E implementations.
The integration test suite is triggered automatically as part of release builds.

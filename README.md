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

### Verify an ASiC-E package

```csharp
IAsiceVerifier verifier = new AsiceVerifier();
using (var inputStream = File.OpenRead("archive.asice"))
{
    // Throws DigestVerificationException or SignatureVerificationException if the package is invalid
    var manifest = verifier.Verify(inputStream);
    var signer = manifest?.certificate.First();
}
```

What the verification covers:
* The digest of every file matches the digest declared in the manifest.
* The CAdES signature (`.p7s`) matches the manifest. Every signer in the signature must be valid.
* If the signature contains a signing time, the signing certificate was valid at that time. The signing time is
  stated by the signer, so it is not proof of when the signature was made.

What it does not cover:
* Trust in the signing certificate. The certificate chain, the CA and revocation (CRL/OCSP) are not checked.
  Check the returned certificate against your own trust store if you need this.
* Packages without a signature. `Verify` returns `null` for these, after the digests have been checked.
* XAdES signatures.

`AsiceReadModel.VerifiedManifest()` checks the signature only. To check the digests, read every entry and
call `DigestVerifier.Verification()`, or use `AsiceVerifier`, which does both.

## File names in packages

`AsiceReadEntry.FullName` is the path of a file inside the package, as the sender wrote it, and
`AsiceReadEntry.FileName` is the name of the file without the folders it is in. The reader follows the ZIP
specification, which ETSI EN 319 162-1 refers to, and rejects packages where a path is absolute, has a drive
letter or uses `\` as separator. It also rejects paths with empty, `.` or `..` segments, and packages where two
files have the same `FileName` and one of them is in a folder, as they could overwrite each other if you use
`FileName` as a key. `AsiceBuilder.AddFile` and `AddFileWithPath` reject the same names.

The reader does not check that a name is legal on the system you write to. On Windows, for example, `NUL` is
a device and `what?.pdf` is not a legal file name. Check the full path before you write a file under it:

```csharp
var root = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
foreach (var entry in asice.Entries)
{
    var target = Path.GetFullPath(Path.Combine(root, entry.FullName));
    if (!target.StartsWith(root, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Refusing to write '{entry.FullName}' outside {root}");
    }

    Directory.CreateDirectory(Path.GetDirectoryName(target));
    // write entry.OpenStream() to target
}
```

Never pass a name from a package to a shell.

## Logging

The library logs through `Microsoft.Extensions.Logging`. Pass an `ILoggerFactory` to
`AsiceReader`, `AsiceVerifier`, `SignatureVerifier` or `AsiceBuilder.Create` to get log
messages, for example the warning when a signing certificate has expired. Without a logger
factory, nothing is logged.

```csharp
var reader = new AsiceReader(loggerFactory);
var verifier = new AsiceVerifier(loggerFactory);
var builder = AsiceBuilder.Create(
    outStream, MessageDigestAlgorithm.SHA256, ManifestSpec.Cades, certificateHolder,
    null, null, loggerFactory);
```

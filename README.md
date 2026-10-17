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

`AsiceReadEntry.FullName` is the path of a file inside the package, as the sender wrote it. The reader checks
that every entry path is *valid*: relative, with `/` as separator, without empty, `.` or `..` segments, and
without control characters or line separators. A valid path combined with a directory using `Path.Combine`
stays inside that directory. `AsiceReader.Read` rejects packages with other paths, with a file and a folder
of the same name, or with two files that differ only in Unicode normalisation. `AsiceBuilder.AddFileWithPath`
rejects the same names.

A valid path is not always a legal file name on every operating system. `AsiceEntryPath.IsPortable` also
rejects `: * ? " < > |`, segments that end with `.` or a space, Windows device names such as `CON` and
`COM1`, bidirectional control characters, and segments over 255 bytes or paths over 4096 bytes. The reader
and the builder accept such names, because they are legal on some systems, and log a warning. Two files in
the root folder whose names differ only in case are accepted with a warning too; they are the same file on
Windows and macOS. Check the name before you write a file under it:

```csharp
foreach (var entry in asice.Entries)
{
    if (!AsiceEntryPath.IsPortable(entry.FullName))
    {
        throw new InvalidOperationException(
            $"Refusing to write '{entry.FullName}': {AsiceEntryPath.Describe(entry.FullName)}");
    }

    var target = Path.Combine(outputDirectory, entry.FullName);
    // write entry.OpenStream() to target
}
```

The rules do not make a name safe for a shell (`~`, `$`, `;` and backticks are allowed), do not look at what
already exists at the target, such as symbolic links, do not normalise Unicode, and do not know the limits of
the file system you write to.

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

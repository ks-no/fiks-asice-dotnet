using System.Diagnostics;
using System.IO;
using System.Text;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test;

// Runs without parallelization so that threads started by other tests do not affect the thread count
[Collection(nameof(LoggerFactoryTest))]
public class LoggerFactoryTest
{
    private const int Iterations = 100;

    // Well below Iterations, so that a leak of one thread per iteration is detected
    private const int MaxThreadIncrease = Iterations / 2;

    [Fact(DisplayName = "Building archives without a logger factory does not leak threads")]
    public void BuildDoesNotLeakThreads()
    {
        var certificate = TestdataLoader.ReadCertificatesForTest();
        BuildArchive(certificate, null);

        var threadsBefore = CurrentThreadCount();
        for (var i = 0; i < Iterations; i++)
        {
            BuildArchive(certificate, null);
        }

        (CurrentThreadCount() - threadsBefore).ShouldBeLessThan(MaxThreadIncrease);
    }

    [Fact(DisplayName = "Reading and verifying archives without a logger factory does not leak threads")]
    public void ReadDoesNotLeakThreads()
    {
        var archive = BuildArchive(TestdataLoader.ReadCertificatesForTest(), null);
        ReadArchive(archive, null);

        var threadsBefore = CurrentThreadCount();
        for (var i = 0; i < Iterations; i++)
        {
            ReadArchive(archive, null);
        }

        (CurrentThreadCount() - threadsBefore).ShouldBeLessThan(MaxThreadIncrease);
    }

    [Fact(DisplayName = "Logger factory given to AsiceBuilder reaches AsiceArchive")]
    public void BuilderPassesLoggerFactoryToArchive()
    {
        var loggerFactory = CreateLoggerFactoryMock();

        BuildArchive(TestdataLoader.ReadCertificatesForTest(), loggerFactory.Object);

        loggerFactory.Verify(f => f.CreateLogger(typeof(AsiceArchive).FullName), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "Logger factory given to AsiceReader reaches DigestVerifier")]
    public void ReaderPassesLoggerFactoryToDigestVerifier()
    {
        var archive = BuildArchive(TestdataLoader.ReadCertificatesForTest(), null);
        var loggerFactory = CreateLoggerFactoryMock();

        ReadArchive(archive, loggerFactory.Object);

        loggerFactory.Verify(f => f.CreateLogger(typeof(DigestVerifier).FullName), Times.AtLeastOnce);
    }

    private static Mock<ILoggerFactory> CreateLoggerFactoryMock()
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);
        return loggerFactory;
    }

    private static byte[] BuildArchive(ICertificateHolder certificate, ILoggerFactory loggerFactory)
    {
        using var zipStream = new MemoryStream();
        using (var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("content")))
        using (var asiceBuilder = AsiceBuilder.Create(
                   zipStream,
                   MessageDigestAlgorithm.SHA256,
                   ManifestSpec.Cades,
                   certificate,
                   null,
                   null,
                   loggerFactory))
        {
            asiceBuilder.AddFile(fileStream, "file.txt");
        }

        return zipStream.ToArray();
    }

    private static void ReadArchive(byte[] archive, ILoggerFactory loggerFactory)
    {
        using var inputStream = new MemoryStream(archive);
        using var asice = new AsiceReader(loggerFactory).Read(inputStream);
        foreach (var entry in asice.Entries)
        {
            using var entryStream = entry.OpenStream();
            entryStream.CopyTo(Stream.Null);
        }

        asice.DigestVerifier.Verification().AllValid.ShouldBeTrue();
        asice.VerifiedManifest().ShouldNotBeNull();

        using var verifyStream = new MemoryStream(archive);
        new AsiceVerifier(loggerFactory).Verify(verifyStream).ShouldNotBeNull();
    }

    private static int CurrentThreadCount()
    {
        using var process = Process.GetCurrentProcess();
        return process.Threads.Count;
    }
}

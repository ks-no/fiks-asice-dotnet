using System;
using System.Collections.Generic;
using System.Linq;
using KS.Fiks.ASiC_E.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KS.Fiks.ASiC_E.Crypto;

public class DigestVerifier : IDigestReceiver, IDigestVerifier
{
    private readonly IDictionary<string, DeclaredDigestFile> _declaredDigests;
    private readonly Queue<string> _validFiles = new Queue<string>();
    private readonly Queue<string> _invalidFiles = new Queue<string>();
    private readonly HashSet<DeclaredDigestFile> _receivedFiles = new HashSet<DeclaredDigestFile>();
    private readonly ILogger<DigestVerifier> _logger;

    private DigestVerifier(IDictionary<string, DeclaredDigestFile> declaredDigests, ILoggerFactory loggerFactory)
    {
        _declaredDigests = declaredDigests ?? throw new ArgumentNullException(nameof(declaredDigests));
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<DigestVerifier>();
    }

    public static DigestVerifier Create(IDictionary<string, DeclaredDigestFile> declaredDigests)
    {
        return Create(declaredDigests, null);
    }

    public static DigestVerifier Create(IDictionary<string, DeclaredDigestFile> declaredDigests, ILoggerFactory loggerFactory)
    {
        var dd = declaredDigests ?? throw new ArgumentNullException(nameof(declaredDigests));
        return new DigestVerifier(dd, loggerFactory);
    }

    public void ReceiveDigest(string fileName, byte[] digest)
    {
        _logger.LogDebug("Got digest for file {fileName}", fileName);
        if (_declaredDigests.TryGetValue(fileName, out var declaredDigest))
        {
            _receivedFiles.Add(declaredDigest);
            if (declaredDigest.Digest.SequenceEqual(digest))
            {
                _logger.LogDebug("Digest verified for file {fileName}", fileName);
                _validFiles.Enqueue(fileName);
            }
            else
            {
                _logger.LogDebug("Digest did not match the declared digest for file {fileName}", fileName);
                _invalidFiles.Enqueue(fileName);
            }
        }
        else
        {
            _logger.LogDebug("No file named {fileName} has been declared. It will be deemed invalid", fileName);
            _invalidFiles.Enqueue(fileName);
        }
    }

    public DigestVerificationResult Verification()
    {
        var totalFilesProcessedCount = _invalidFiles.Count + _validFiles.Count;
        if (totalFilesProcessedCount != _declaredDigests.Count)
        {
            throw new DigestVerificationException($"Total number of files processed by the verifier was {totalFilesProcessedCount}, but {_declaredDigests.Count} files was declared");
        }

        var missingFiles = _declaredDigests.Where(d => !_receivedFiles.Contains(d.Value)).Select(d => d.Key).ToList();
        if (missingFiles.Count > 0)
        {
            throw new DigestVerificationException($"The following declared files were not processed by the verifier: {string.Join(",", missingFiles)}");
        }

        return new DigestVerificationResult(_validFiles, _invalidFiles);
    }
}
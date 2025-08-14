using System.Collections.Generic;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Manifest
{
    public interface IManifestCreator
    {
        /// <summary>
        /// Creates a <see cref="ManifestContainer"/> with a finished, serialized
        /// manifest file, containing information on the enumerated entries, and
        /// optionally a reference to the signature file. The
        /// <see cref="ManifestContainer"/> will have the provided filename
        /// registered, which will determine the filename of the manifest
        /// in the ASiC container's internal file tree structure.
        /// </summary>
        /// <param name="entries">The entries to list in the manifest.</param>
        /// <param name="signatureFileRef">An object that holds information
        /// about the signature file. If null, the signature reference
        /// will be omitted from the serialized manifest and the
        /// <see cref="ManifestContainer.SignatureFileRef"/> property
        /// in the returned object will be null.</param>
        /// <returns>A <see cref="ManifestContainer"/>.</returns>
        ManifestContainer CreateManifest(
            IEnumerable<AsicePackageEntry> entries,
            SignatureFileRef signatureFileRef);
    }
}
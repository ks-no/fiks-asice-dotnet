using System;
using System.Collections.Generic;

namespace KS.Fiks.ASiC_E.Model
{
    public class Signatures
    {
        public IEnumerable<SignatureFileContainer> Containers { get; }

        public Signatures(IEnumerable<SignatureFileContainer> containers)
        {
            Containers = containers ?? throw new ArgumentNullException(nameof(containers));
        }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace KS.Fiks.ASiC_E.Model
{
    public class DigestVerificationResult
    {
        private IEnumerable<string> _validElements;
        private IEnumerable<string> _invalidElements;

        public DigestVerificationResult(IEnumerable<string> validElements, IEnumerable<string> invalidElements)
        {
            this._validElements = validElements;
            this._invalidElements = invalidElements;
        }

        public bool AllValid => !this._invalidElements.Any();

        public IEnumerable<string> ValidElements => this._validElements.ToList();

        public IEnumerable<string> InvalidElements => this._invalidElements.ToList();
    }
}
namespace KS.Fiks.ASiC_E.Model
{
    public class FileRef
    {
        public string FileName { get; }

        public MimeType MimeType { get; }

        public FileRef(string fileName, MimeType mimeType)
        {
            FileName = fileName;
            MimeType = mimeType;
        }
    }
}
namespace PhotoViewer.Core.Exceptions
{
    public class CodecNotFoundException : Exception
    {

        public string FileType { get; }

        public CodecNotFoundException(string fileType, Exception innerException)
            : base("No codec was found for the specified file type..", innerException)
        {
            FileType = fileType;
        }

    }

}

using Espera.Mobile.Core;

namespace Espera.Android
{
    public class File : IFile
    {
        public byte[] ReadAllBytes(string path) => System.IO.File.ReadAllBytes(path);
    }
}
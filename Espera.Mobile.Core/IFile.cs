namespace Espera.Mobile.Core
{
    public interface IFile
    {
        byte[] ReadAllBytes(string path);
    }
}
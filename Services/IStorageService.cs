using System.Threading.Tasks;

namespace Project_Nook.Services
{
    public interface IStorageService
    {
        Task<string> CopyFileToStorageAsync(string sourcePath);
        Task<string> SaveTextToStorageAsync(string text, string hintName);
        void DeleteFromStorage(string storedPath);
        void OpenFile(string storedPath);
        void RevealInExplorer(string storedPath);
    }
}

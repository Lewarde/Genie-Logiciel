using System.Threading.Tasks;

namespace EasySave.Core
{
    /// <summary>
    /// Interface for handling interactive menu operations
    /// </summary>
    public interface IMenuManager
    {
        /// <summary>
        /// Starts the interactive menu system
        /// </summary>
        Task StartInteractiveMenuAsync();
    }
}
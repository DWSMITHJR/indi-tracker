using System;
using System.Threading.Tasks;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public interface IToastService
    {
        event Action<Toast> OnShow;
        event Action<Guid> OnHide;
        
        void ShowInfo(string message, string title = "Info", bool autoClose = true);
        void ShowSuccess(string message, string title = "Success", bool autoClose = true);
        void ShowWarning(string message, string title = "Warning", bool autoClose = true);
        void ShowError(string message, string title = "Error", bool autoClose = true);
        void ShowToast(Toast toast);
        void HideToast(Guid toastId);
    }
}

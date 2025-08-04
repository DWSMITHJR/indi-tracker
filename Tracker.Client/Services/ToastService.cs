using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public class ToastService : IToastService
    {
        public event Action<Toast> OnShow;
        public event Action<Guid> OnHide;

        public void ShowInfo(string message, string title = "Info", bool autoClose = true)
        {
            var toast = new Toast
            {
                Title = title,
                Message = message,
                Level = ToastLevel.Info,
                AutoClose = autoClose
            };
            ShowToast(toast);
        }

        public void ShowSuccess(string message, string title = "Success", bool autoClose = true)
        {
            var toast = new Toast
            {
                Title = title,
                Message = message,
                Level = ToastLevel.Success,
                AutoClose = autoClose
            };
            ShowToast(toast);
        }

        public void ShowWarning(string message, string title = "Warning", bool autoClose = true)
        {
            var toast = new Toast
            {
                Title = title,
                Message = message,
                Level = ToastLevel.Warning,
                AutoClose = autoClose
            };
            ShowToast(toast);
        }

        public void ShowError(string message, string title = "Error", bool autoClose = true)
        {
            var toast = new Toast
            {
                Title = title,
                Message = message,
                Level = ToastLevel.Error,
                AutoClose = autoClose
            };
            ShowToast(toast);
        }

        public void ShowToast(Toast toast)
        {
            if (toast == null)
                return;

            OnShow?.Invoke(toast);

            if (toast.AutoClose)
            {
                Task.Delay(toast.AutoCloseDelay).ContinueWith(_ =>
                {
                    HideToast(toast.Id);
                });
            }
        }

        public void HideToast(Guid toastId)
        {
            OnHide?.Invoke(toastId);
        }
    }
}

using System;
using System.Timers;

namespace Mokit.Web.Services;

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class ToastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastLevel Level { get; set; } = ToastLevel.Info;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public interface IToastService
{
    event Action<ToastMessage>? OnShow;
    void ShowToast(string message, ToastLevel level);
    void ShowSuccess(string message, string title = "Success");
    void ShowError(string message, string title = "Error");
    void ShowInfo(string message, string title = "Info");
    void ShowWarning(string message, string title = "Warning");
}

public class ToastService : IToastService
{
    public event Action<ToastMessage>? OnShow;

    public void ShowToast(string message, ToastLevel level)
    {
        var toast = new ToastMessage
        {
            Message = message,
            Level = level
        };
        
        OnShow?.Invoke(toast);
    }

    public void ShowSuccess(string message, string title = "Success")
    {
        OnShow?.Invoke(new ToastMessage { Title = title, Message = message, Level = ToastLevel.Success });
    }

    public void ShowError(string message, string title = "Error")
    {
        OnShow?.Invoke(new ToastMessage { Title = title, Message = message, Level = ToastLevel.Error });
    }

    public void ShowInfo(string message, string title = "Info")
    {
        OnShow?.Invoke(new ToastMessage { Title = title, Message = message, Level = ToastLevel.Info });
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        OnShow?.Invoke(new ToastMessage { Title = title, Message = message, Level = ToastLevel.Warning });
    }
}

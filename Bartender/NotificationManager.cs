using Dalamud.Interface.ImGuiNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender;

public static class NotificationManager
{
    public static void Display(
        string localizedString,
        NotificationType notificationType = NotificationType.Success,
        double durationInSeconds = 3
    )
    {
        DalamudApi.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
        {
            Content = localizedString,
            Type = notificationType,
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(durationInSeconds)
        });
    }
}

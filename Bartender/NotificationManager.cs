using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender;

public static class NotificationManager
{
    public static void Display(string localizedString, Dalamud.Interface.Internal.Notifications.NotificationType notificationType, double durationInSeconds)
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

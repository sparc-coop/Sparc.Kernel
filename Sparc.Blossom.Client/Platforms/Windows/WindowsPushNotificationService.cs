﻿using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Realtime;

public class WindowsPushNotificationService : IPushNotificationService
{
    public IDevice Device { get; }
    public NavigationManager Nav { get; }

    public WindowsPushNotificationService(IDevice device, NavigationManager nav)
    {
        Device = device;
        Nav = nav;
    }

    public void OnMessageReceived(string url)
    {
        throw new NotImplementedException();
    }

    public void OnNewToken(string token)
    {
        throw new NotImplementedException();
    }
}

﻿using Android.Provider;

namespace Sparc.Platforms.Maui;

public class AndroidDevice : Core.Device
{
    private string _id;
    public override string Id
    {
        get
        {
            if (_id != null) return _id;
            try
            {
                _id = Settings.Secure.GetString(global::Android.App.Application.Context.ContentResolver, Settings.Secure.AndroidId);
            }
            catch
            { }

            return _id;
        }
        set { _id = value; }
    }

    private string _pushToken;
    public override string PushToken
    {
        get
        {
            if (_pushToken != null) return _pushToken;

            _pushToken = SecureStorage.GetAsync("sparc-device-pushtoken").Result;
            return _pushToken;
        }
        set
        {
            _pushToken = value;
            SecureStorage.SetAsync("sparc-device-pushtoken", value).Wait();
        }
    }

    public override string Platform => Core.Platforms.Android;
}

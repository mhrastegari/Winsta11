﻿using System;

namespace WinstaCore
{
    public static class AppCore
    {
        public static IServiceProvider Container { get; private set; }

        public static void SetContainer(IServiceProvider serviceProvider)
        {
            Container = serviceProvider;
        }

    }
}

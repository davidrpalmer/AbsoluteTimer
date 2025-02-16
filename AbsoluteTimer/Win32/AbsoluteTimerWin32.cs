﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace AbsoluteTimer.Win32
{
    internal sealed class AbsoluteTimerWin32 : IDisposable
    {
        static readonly int FileTimeSize = Marshal.SizeOf(typeof(FILETIME));

        readonly object _state;
        IntPtr _timer;
        IntPtr _dueTime;
        Interop.TimerCallback _callback;

        public AbsoluteTimerWin32(DateTime dt, Action<object> callback, object state)
        {
            _state = state;
            _callback = CallbackToTimerCallback(callback);
            _timer = Interop.CreateThreadpoolTimer(_callback, IntPtr.Zero, IntPtr.Zero);
            _dueTime = Marshal.AllocHGlobal(FileTimeSize);
            Marshal.StructureToPtr(DateTimeToFILETIME(dt.ToUniversalTime()), _dueTime, false);
            Interop.SetThreadpoolTimer(_timer, _dueTime, 0, 0);
        }

        public void Dispose()
        {
            ReleaseTimer();
            GC.SuppressFinalize(this);
        }

        ~AbsoluteTimerWin32()
        {
            ReleaseTimer();
        }

        void ReleaseTimer()
        {
            var timer = Interlocked.Exchange(ref _timer, IntPtr.Zero);
            if (timer != IntPtr.Zero)
            {
                Interop.CloseThreadpoolTimer(timer);
                Marshal.FreeHGlobal(_dueTime);
                _dueTime = IntPtr.Zero;
            }
        }

        Interop.TimerCallback CallbackToTimerCallback(Action<object> callback)
        {
            return new Interop.TimerCallback((i, c, t) =>
            {
                callback(_state);
            });
        }

        static FILETIME DateTimeToFILETIME(DateTime time)
        {
            FILETIME ft;
            var value = time.ToFileTimeUtc();
            ft.dwLowDateTime = (int)(value & 0xFFFFFFFF);
            ft.dwHighDateTime = (int)(value >> 32);
            return ft;
        }
    }
}

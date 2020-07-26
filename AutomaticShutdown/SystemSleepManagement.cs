using System;
using System.Runtime.InteropServices;

namespace AutomaticShutdown
{
    class SystemSleepManagement
    {
        //定义API函数
        [DllImport("kernel32.dll")]
        static extern uint SetThreadExecutionState(ExecutionFlag flags);

        [Flags]
        enum ExecutionFlag : uint
        {
            System = 0x00000001,
            Display = 0x00000002,
            Continuous = 0x80000000,
        }

        /// <summary>
        ///阻止系统休眠，直到线程结束恢复休眠策略
        /// </summary>
        /// <param name="includeDisplay">是否阻止关闭显示器</param>
        public static void PreventSleep(bool includeDisplay = false)
        {
            if (includeDisplay)
                SetThreadExecutionState(ExecutionFlag.System | ExecutionFlag.Display | ExecutionFlag.Continuous);
            else
                SetThreadExecutionState(ExecutionFlag.System | ExecutionFlag.Continuous);
        }

        /// <summary>
        ///恢复系统休眠策略
        /// </summary>
        public static void RestoreSleep()
        {
            SetThreadExecutionState(ExecutionFlag.Continuous);
        }

        /// <summary>
        ///重置系统休眠计时器
        /// </summary>
        /// <param name="includeDisplay">是否阻止关闭显示器</param>
        public static void ResetSleepTimer(bool includeDisplay = false)
        {
            if (includeDisplay)
                SetThreadExecutionState(ExecutionFlag.System | ExecutionFlag.Display);
            else
                SetThreadExecutionState(ExecutionFlag.System);
        }
    }
}

using System.Runtime.CompilerServices;
using UnityEngine;

namespace JJ4Unity.Runtime.Extension
{
    public static class Debug
    {
        public static bool isDebugBuild
        {
            get { return UnityEngine.Debug.isDebugBuild; }
        }

        private static void ConvertLog(
            object message,
            string memberName,
            string filePath,
            int lineNumber,
            System.Action<object> logFunction
        )
        {
            var convertPath = filePath.Replace("/", "\\");
            var lastIndexOf = convertPath.LastIndexOf('\\') + 1;
            var length = filePath.Length - lastIndexOf - 3;
            logFunction.Invoke(
                $"<b>[{filePath.Substring(lastIndexOf, length)}::{memberName}:L{lineNumber.ToString()}:T{Time.frameCount.ToString()}]</b> {message}");
        }

        [System.Diagnostics.Conditional("__DEBUG__")]
        public static void Log(
            object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            ConvertLog(message, memberName, filePath, lineNumber, UnityEngine.Debug.Log);
        }

        [System.Diagnostics.Conditional("__DEBUG__")]
        public static void LogWarning(
            object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            ConvertLog(message, memberName, filePath, lineNumber, UnityEngine.Debug.LogWarning);
        }

        [System.Diagnostics.Conditional("__DEBUG__")]
        public static void LogError(
            object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            ConvertLog(message, memberName, filePath, lineNumber, UnityEngine.Debug.LogError);
        }

        [System.Diagnostics.Conditional("__DEBUG__")]
        public static void DrawLine(
            UnityEngine.Vector3 start,
            UnityEngine.Vector3 end,
            UnityEngine.Color color = default(UnityEngine.Color),
            float duration = 0f,
            bool depthTest = true
        )
        {
            UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
        }

        [System.Diagnostics.Conditional("__DEBUG__")]
        public static void DrawRay(
            UnityEngine.Vector3 start,
            UnityEngine.Vector3 dir,
            UnityEngine.Color color = default(UnityEngine.Color),
            float duration = 0f,
            bool depthTest = true
        )
        {
            UnityEngine.Debug.DrawRay(start, dir, color, duration, depthTest);
        }

        [System.Diagnostics.Conditional("__DEBUG__")]
        public static void Assert(
            bool condition,
            object message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            var convertPath = filePath.Replace("/", "\\");
            var lastIndexOf = convertPath.LastIndexOf('\\') + 1;
            var length = filePath.Length - lastIndexOf - 3;
            UnityEngine.Debug.Assert(condition,
                $"<b>[{filePath.Substring(lastIndexOf, length)}::{memberName}:L{lineNumber}]</b> {message}");
        }
    }
}

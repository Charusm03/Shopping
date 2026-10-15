using System;
using System.Collections.Generic;
using System.Text;

namespace ShoppingApplication.service
{
    internal enum NoticeLevel
    {
        Success,
        Warning,
        Error
    }

    internal record Notice(int Id, NoticeLevel Level, string Message);

    // Messages the CUSTOMER is meant to read on screen (screen only, nothing is written to a file here).
    //   Logger   = everything, for developers / support (file).
    //   Notifier = short, friendly messages only - never exceptions, stack traces or internal details.
    internal static class Notifier
    {
        private const int Capacity = 20;
        private static readonly List<Notice> _notices = new();
        private static int _lastId;

        public static void Success(string message) => Add(NoticeLevel.Success, message);
        public static void Warning(string message) => Add(NoticeLevel.Warning, message);
        public static void Error(string message) => Add(NoticeLevel.Error, message);

        // Newest messages, oldest first
        public static IReadOnlyList<Notice> Latest(int count) => _notices.TakeLast(count).ToList();

        // A page can remember Mark when it opens and later ask only for messages raised on that page
        public static int Mark => _lastId;
        public static Notice? LastAfter(int mark) => _notices.LastOrDefault(n => n.Id > mark);

        // Called on logout so the next customer never sees the previous customer's messages
        public static void Clear() => _notices.Clear();

        private static void Add(NoticeLevel level, string message)
        {
            _notices.Add(new Notice(++_lastId, level, message));
            if (_notices.Count > Capacity)
            {
                _notices.RemoveAt(0);
            }
        }
    }
}

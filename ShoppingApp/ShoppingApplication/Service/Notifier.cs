namespace ShoppingApplication.Service
{
    internal enum NoticeLevel
    {
        Success,
        Warning,
        Error
    }

    internal record Notice(int Id, NoticeLevel Level, string Message);

    // Messages the CUSTOMER reads on screen (screen only; the technical log stays in Logger).
    internal static class Notifier
    {
        private const int Capacity = 20;
        private static readonly List<Notice> _notices = new();
        private static readonly object _lock = new();
        private static int _lastId;

        // A page remembers Mark when it opens, then asks only for messages raised on that page
        public static int Mark
        {
            get
            {
                lock (_lock)
                {
                    return _lastId;
                }
            }
        }

        public static void Success(string message) => Add(NoticeLevel.Success, message);
        public static void Warning(string message) => Add(NoticeLevel.Warning, message);
        public static void Error(string message) => Add(NoticeLevel.Error, message);

        // Newest messages, oldest first
        public static List<Notice> Latest(int count)
        {
            lock (_lock)
            {
                return _notices.TakeLast(count).ToList();
            }
        }

        // Messages raised after the given mark
        public static List<Notice> Since(int mark, int count)
        {
            lock (_lock)
            {
                return _notices.Where(n => n.Id > mark).TakeLast(count).ToList();
            }
        }

        // Called on logout so the next customer never sees the previous customer's messages
        public static void Clear()
        {
            lock (_lock)
            {
                _notices.Clear();
            }
        }

        private static void Add(NoticeLevel level, string message)
        {
            lock (_lock)
            {
                _notices.Add(new Notice(++_lastId, level, message));
                if (_notices.Count > Capacity)
                {
                    _notices.RemoveAt(0);
                }
            }
        }
    }
}

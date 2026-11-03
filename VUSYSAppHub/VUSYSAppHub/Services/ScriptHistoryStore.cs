using VUSYSAppHub.Contracts;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.Services
{
    public class ScriptHistoryStore : IScriptHistoryStore
    {
        private readonly List<ScriptHistoryEntry> _entries = new();
        private readonly object _sync = new();

        public void Add(ScriptHistoryEntry entry)
        {
            lock (_sync)
            {
                _entries.Insert(0, entry);

                if (_entries.Count > 50)
                {
                    _entries.RemoveAt(_entries.Count - 1);
                }
            }
        }

        public List<ScriptHistoryEntry> GetAll()
        {
            lock (_sync)
            {
                return _entries.ToList();
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _entries.Clear();
            }
        }
    }
}
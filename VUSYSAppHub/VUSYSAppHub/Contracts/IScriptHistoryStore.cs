using VUSYSAppHub.Models;

namespace VUSYSAppHub.Contracts
{
    public interface IScriptHistoryStore
    {
        void Add(ScriptHistoryEntry entry);
        List<ScriptHistoryEntry> GetAll();
        void Clear();
    }
}

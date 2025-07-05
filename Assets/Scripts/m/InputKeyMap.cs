using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class InputKeyMap
{
    public List<Entry> entries = new();

    [System.Serializable]
    public class Entry
    {
        public string key;
        public List<int> values;
    }

    public Dictionary<string, List<int>> KeyBindings
    {
        get
        {
            return entries.ToDictionary(e => e.key, e => e.values);
        }
        set
        {
            entries.Clear();
            foreach (var kv in value)
            {
                entries.Add(new Entry
                {
                    key = kv.Key,
                    values = kv.Value
                });
            }
        }
    }
}

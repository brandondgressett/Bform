namespace BFormDomain.CommonCode.Utility;

public class MultiMap<K, V>
    where K: notnull
{
    private readonly Dictionary<K, List<V>> _dictionary = new();

    public void Clear()
    {
        _dictionary.Clear();
    }

    public void Add(K key, V value)
    {
        // Add a key.
        if (this._dictionary.TryGetValue(key, out var list))
        {
            list.Add(value);
        }
        else
        {
            list = new List<V>
            {
                value
            };
            this._dictionary[key] = list;
        }
    }

    public IEnumerable<K> Keys
    {
        get
        {

            return this._dictionary.Keys;
        }
    }

    public List<V>? this[K key]
    {
        get
        {
            // Get list at a key.
            if (!this._dictionary.TryGetValue(key, out List<V>? list))
            {
                list = new List<V>();
                this._dictionary[key] = list;
            }
            return list;
        }
    }
}
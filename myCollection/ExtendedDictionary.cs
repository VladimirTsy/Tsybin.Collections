
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;



namespace myCollection
{



    //Структура составного ключа.
    public struct KeysPair<TId, TName>
    {
        public TId id;  
        public TName name;
        public KeysPair(TId id, TName name) : this()
        {
            this.id = id;
            this.name = name;
        }

        public override bool Equals(object ob) 
        {
            if (ob is KeysPair<TId, TName>)
            {
                KeysPair<TId, TName> c = (KeysPair<TId, TName>)ob;
                return id.Equals(c.id) && name.Equals(c.name);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()                  
        {
            return id.GetHashCode() ^ name.GetHashCode();                                                    
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TName"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ExtendedDictionary<TId, TName, TValue> : IDictionary<KeysPair<TId, TName>, TValue>
    {
        private Dictionary<KeysPair<TId, TName>, TValue> fullDict;

        private Dictionary<TId, List<TValue>> fastAccesByIdDict;

        private Dictionary<TName, List<TValue>> fastAccesByNameDict;

        private ReaderWriterLockSlim rw_locker;

        public ExtendedDictionary(int size = 0)
        {
            size = size >= 0 ? size : 0;
            fullDict = new Dictionary<KeysPair<TId, TName>, TValue>(size);
            fastAccesByIdDict = new Dictionary<TId, List<TValue>>(size);
            fastAccesByNameDict = new Dictionary<TName, List<TValue>>(size);

            rw_locker = new ReaderWriterLockSlim();
        }

        #region

        public TValue[] GetByName(TName name)
        {
            try
            {
                rw_locker.EnterReadLock();
                return fastAccesByNameDict[name].ToArray();
            }
            finally
            {
                rw_locker.ExitReadLock();
            }
        }

        public TValue[] GetById(TId id)
        {
            try
            {
                rw_locker.EnterReadLock();
                return fastAccesByIdDict[id].ToArray();
            }
            finally
            {
                rw_locker.ExitReadLock();
            }
        }

        //Основной метод модификации коллекции
        public void Add(KeysPair<TId, TName> key, TValue value)
        {
            try
            {
                rw_locker.EnterWriteLock();

                fullDict[key] = value; //Добавляем запись в основной словарь

                if (fastAccesByIdDict.ContainsKey(key.id))
                    fastAccesByIdDict[key.id].Add(value); //Добавляем запись в быстрый кэш по id
                else
                    fastAccesByIdDict[key.id] = new List<TValue>() {value};

                if (fastAccesByNameDict.ContainsKey(key.name))
                    fastAccesByNameDict[key.name].Add(value); //Добавляем запись в быстрый кэш по Name
                else
                    fastAccesByNameDict[key.name] = new List<TValue>() {value};
            }
            finally
            {
                rw_locker.ExitWriteLock();
            }
        }

        public bool Remove(KeysPair<TId, TName> key)
        {
            try
            {
                rw_locker.EnterUpgradeableReadLock();

                TId id = key.id;
                TName name = key.name;

                if (fullDict.ContainsKey(new KeysPair<TId, TName>(id, name))) //Если такой ключ есть
                {
                    try
                    {
                        rw_locker.EnterWriteLock(); //Захватываем лок на модификацию.

                        var tmp_key = new KeysPair<TId, TName>(id, name);

                        if (fastAccesByIdDict[id].Count > 1) //Убираем элемент из быстрого доступа по id
                            fastAccesByIdDict[id].Remove(fullDict[tmp_key]);
                        else fastAccesByIdDict.Remove(id);

                        if (fastAccesByNameDict[name].Count > 1) //Убираем его из быстрого доступа по name
                            fastAccesByNameDict[name].Remove(fullDict[tmp_key]);
                        else fastAccesByNameDict.Remove(name);

                        fullDict.Remove(tmp_key); //Убираем его окончательно из основного словаря.

                        return true;
                    }
                    finally
                    {
                        rw_locker.ExitWriteLock(); //Снимаем лок модификации
                    }
                }

                return false;
            }
            finally
            {
                rw_locker.ExitUpgradeableReadLock(); //Снимаем лок чтения
            }
        }

        public TValue this[TId id, TName name]
        {
            set { this.Add(id, name, value); }
            get
            {
                try
                {
                    rw_locker.EnterReadLock();
                    return fullDict[new KeysPair<TId, TName>(id, name)];
                }
                finally
                {
                    rw_locker.ExitReadLock();
                }
            }
        }

        public void Clear()
        {
            try
            {
                rw_locker.EnterWriteLock();
                fullDict.Clear();
                fastAccesByIdDict.Clear();
                fastAccesByNameDict.Clear();
            }
            finally
            {
                rw_locker.ExitWriteLock();
            }
        }

        #endregion

        #region

        //Методы, которые ничего не делают, кроме как вызывают один из основных методов.
        public void Add(TId id, TName name, TValue value)
        {
            this.Add(new KeysPair<TId, TName>(id, name), value);
        }

        public void Add(KeyValuePair<KeysPair<TId, TName>, TValue> item)
        {
            this.Add(new KeysPair<TId, TName>(item.Key.id, item.Key.name), item.Value);
        }

        public TValue this[KeysPair<TId, TName> key]
        {
            get
            {
                try
                {
                    rw_locker.EnterReadLock();
                    return this[key.id, key.name];
                }
                finally
                {
                    rw_locker.ExitReadLock();
                }
            }
            set { this.Add(key, value); }
        }

        public bool Remove(TId id, TName name)
        {
            return this.Remove(new KeysPair<TId, TName>(id, name));
        }

        public void CopyTo(KeyValuePair<KeysPair<TId, TName>, TValue>[] array, int arrayIndex)
        {

            try
            {
                rw_locker.EnterReadLock();
                int index = arrayIndex;
                foreach (var item in fullDict)
                {
                    array[index] = item;
                    index++;
                }
            }
            finally
            {
                rw_locker.ExitReadLock();
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region

        public void EnterUpgradeableReadLock()
        {

            rw_locker.EnterUpgradeableReadLock();
        }

        public void ExitUpgradeableReadLock()
        {
            rw_locker.ExitUpgradeableReadLock();
        }

        public int Count
        {
            get
            {
                try
                {
                    rw_locker.EnterReadLock();
                    return fullDict.Count();
                }
                finally
                {
                    rw_locker.ExitReadLock();
                }
            }
        }


        public IEnumerator<KeyValuePair<KeysPair<TId, TName>, TValue>> GetEnumerator()
        {
            try
            {
                rw_locker.EnterReadLock();
                return fullDict.GetEnumerator();
            }
            finally
            {
                rw_locker.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            try
            {
                rw_locker.EnterReadLock();
                return fullDict.GetEnumerator();
            }
            finally
            {
                rw_locker.ExitReadLock();
            }
        }

        public bool ContainsKey(KeysPair<TId, TName> key)
        {
            try
            {
                rw_locker.EnterReadLock();
                return fullDict.ContainsKey(key);
            }
            finally
            {
                rw_locker.ExitReadLock();
            }
        }

        public ICollection<KeysPair<TId, TName>> Keys
        {
            get
            {
                try
                {
                    rw_locker.EnterReadLock();
                    return fullDict.Keys;
                }
                finally
                {
                    rw_locker.ExitReadLock();
                }
            }
        }

        public bool TryGetValue(KeysPair<TId, TName> key, out TValue value)
        {
            try
            {
                rw_locker.EnterReadLock();
                return fullDict.TryGetValue(key, out value);
            }
            finally
            {
                rw_locker.ExitReadLock();
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                try
                {
                    rw_locker.EnterReadLock();
                    return fullDict.Values;
                }
                finally
                {
                    rw_locker.ExitReadLock();
                }
            }
        }

        public bool Contains(KeyValuePair<KeysPair<TId, TName>, TValue> item)
        {
            try
            {
                rw_locker.EnterReadLock();
                return fullDict.Contains(item);
            }
            finally
            {
                rw_locker.ExitReadLock();
            }
        }

        public bool Remove(KeyValuePair<KeysPair<TId, TName>, TValue> item)
        {
            return this.Remove(item.Key);
        }

        #endregion
    }
}

   
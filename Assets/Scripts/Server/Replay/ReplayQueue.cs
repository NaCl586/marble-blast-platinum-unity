using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server.Replay
{
    public class ReplayQueue
    {
        private readonly string _path;

        private readonly List<PendingReplay> _items =
            new List<PendingReplay>();
        public int Count => _items.Count;

        public ReplayQueue()
        {
            _path = Path.Combine(
                Application.persistentDataPath,
                "ReplayQueue.json");

            Load();
        }

        public bool HasPendingReplay =>
            _items.Count > 0;

        public PendingReplay? Peek()
        {
            if (_items.Count == 0)
                return null;

            return _items[0];
        }

        public void Enqueue(
            PendingReplay replay)
        {
            int index =
                _items.FindIndex(x =>
                    x.ScoreId == replay.ScoreId);

            if (index >= 0)
            {
                _items[index] = replay;
            }
            else
            {
                _items.Add(replay);
            }

            Save();
        }

        public void Update(
            PendingReplay replay)
        {
            int index =
                _items.FindIndex(x =>
                    x.ScoreId == replay.ScoreId);

            if (index < 0)
                return;

            _items[index] = replay;

            Save();
        }

        public void Dequeue()
        {
            if (_items.Count == 0)
                return;

            _items.RemoveAt(0);

            Save();
        }

        public void Clear()
        {
            _items.Clear();

            Save();
        }

        private void Save()
        {
            ReplayQueueData data =
                new ReplayQueueData
                {
                    Items = _items
                };

            string json =
                JsonUtility.ToJson(
                    data,
                    true);

            File.WriteAllText(
                _path,
                json);
        }

        private void Load()
        {
            if (!File.Exists(_path))
                return;

            string json =
                File.ReadAllText(_path);

            ReplayQueueData? data =
                JsonUtility.FromJson<ReplayQueueData>(
                    json);

            _items.Clear();

            if (data != null)
            {
                _items.AddRange(
                    data.Items);
            }
        }
    }
}
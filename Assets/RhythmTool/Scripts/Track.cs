﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// A Track is a collection of Features that are ordered in time.
    /// </summary>
    public abstract class Track : ScriptableObject
    {
        public Feature this[int index]
        {
            get
            {
                return GetFeature(index);
            }
        }

        /// <summary>
        /// The number of Features contained in this Track.
        /// </summary>
        public abstract int count { get; }

        /// <summary>
        /// Sorts the Track's Features. Use this after changing a Feature's timestamp.
        /// </summary>
        public abstract void Sort();

        /// <summary>
        /// Add a Feature to the Track.
        /// </summary>
        /// <param name="feature">The Feature to add.</param>
        public abstract void Add(Feature feature);

        /// <summary>
        /// Remove a Feature from the Track.
        /// </summary>
        /// <param name="feature">The Feature to add.</param>
        public abstract void Remove(Feature feature);

        /// <summary>
        /// Returns the index of the Feature that is closest to a timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp in seconds.</param>
        /// <returns>The index of the Feature that is closest to the timestamp.</returns>
        public abstract int GetIndex(float timestamp);

        /// <summary>
        /// Returns the index of the Feature that is closest to a timestamp, Including the feature's length.
        /// </summary>
        /// <param name="timestamp">The timestamp in seconds.</param>
        /// <returns>The index of the Feature that is closest to the timestamp, including the Feature's length.</returns>
        public abstract int GetIntersectingIndex(float timestamp);

        protected abstract Feature GetFeature(int index);
    }

    /// <summary>
    /// A Track is a collection of Features that are ordered in time.
    /// </summary>
    /// <typeparam name="T">The type of Features contained in this Track.</typeparam>
    public abstract class Track<T> : Track where T : Feature
    {
        public new T this[int index]
        {
            get
            {
                lock (_lock)
                    return _features[index];
            }
        }

        /// <summary>
        /// The number of Features contained in this Track.
        /// </summary>
        public override int count
        {
            get
            {
                lock (_lock)
                    return _features.Count;
            }
        }

        private object _lock = new object();

        [SerializeField]
        private List<T> _features = new List<T>();

        [NonSerialized]
        private List<int> cachedTimestamps = new List<int>();
        private Dictionary<int, int> cachedIndices = new Dictionary<int, int>();

        private static Type concreteType;

        static Track()
        {
            foreach (Type type in typeof(T).Assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(Track<T>)) && !type.IsAbstract)
                {
                    concreteType = type;
                    return;
                }
            }
        }

        protected override Feature GetFeature(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Add a Feature to the Track.
        /// </summary>
        /// <param name="feature">The Feature to add.</param>
        public override void Add(Feature feature)
        {
            T _feature = feature as T;

            if (_feature == null)
                throw new ArgumentException("Cannot add " + typeof(T) + " to " + GetType().Name);

            Add(_feature);
        }

        /// <summary>
        /// Add a Feature to the Track.
        /// </summary>
        /// <param name="feature">The Feature to add.</param>
        public void Add(T feature)
        {
            lock (_lock)
            {
                if (_features.Count == 0 || feature.timestamp > _features[_features.Count - 1].timestamp)
                {
                    _features.Add(feature);

                    return;
                }

                int index = GetIndex(feature.timestamp);

                _features.Insert(index, feature);

                ClearCache(feature.timestamp);
            }
        }

        /// <summary>
        /// Remove a Feature from the Track.
        /// </summary>
        /// <param name="feature">The Feature to remove.</param>
        public override void Remove(Feature feature)
        {
            Remove(feature as T);
        }

        /// <summary>
        /// Remove a Feature from the Track.
        /// </summary>
        /// <param name="feature">The Feature to remove.</param>
        public void Remove(T feature)
        {
            lock (_lock)
            {
                _features.Remove(feature);

                ClearCache(feature.timestamp);
            }
        }

        /// <summary>
        /// Sorts the Track's Features. Use this after changing a Feature's timestamp.
        /// </summary>
        public override void Sort()
        {
            lock (_lock)
            {
                _features.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

                ClearCache(0);
            }
        }

        /// <summary>
        /// Finds all Features within a certain time frame.
        /// </summary>
        /// <param name="features">The list of Features to populate</param>
        /// <param name="start">The starting point in seconds.</param>
        /// <param name="end">The end point in seconds.</param>
        public void GetFeatures(List<T> features, float start, float end)
        {
            int startIndex = GetIndex(start);
            int endIndex = GetIndex(end);

            lock (_lock)
            {
                for (int i = startIndex; i < endIndex; i++)
                    features.Add(_features[i]);
            }
        }

        /// <summary>
        /// Finds all Features within a certain time frame, including Features with a length that intersects the time frame.
        /// </summary>
        /// <param name="features">The list of Features to populate</param>
        /// <param name="start">The starting point in seconds.</param>
        /// <param name="end">The end point in seconds.</param>
        public void GetIntersectingFeatures(List<T> features, float start, float end)
        {
            int startIndex = GetIntersectingIndex(start);
            int endIndex = GetIndex(end);

            lock (_lock)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    T feature = _features[i];

                    if (feature.timestamp + feature.length > start)
                        features.Add(feature);
                }
            }
        }

        /// <summary>
        /// Returns the index of the Feature that is closest to a timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp in seconds.</param>
        /// <returns>The index of the Feature that is closest to the timestamp.</returns>
        public override int GetIndex(float timestamp)
        {
            lock (_lock)
            {
                int index = BinarySearch(timestamp);

                if (index < 0)
                    index = ~index;

                while (index > 1 && _features[index - 1].timestamp >= timestamp)
                    index--;

                return index;
            }
        }

        /// <summary>
        /// Returns the index of the Feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <returns>The index of the Feature.</returns>
        public int GetIndex(T feature)
        {
            int index = GetIndex(feature.timestamp);

            lock (_lock)
            {
                for (int i = index; i < _features.Count; i++)
                {
                    if (_features[i] == feature)
                        return i;

                    if (_features[i].timestamp > feature.timestamp)
                        break;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the Feature that is closest to a timestamp, Including the feature's length.
        /// </summary>
        /// <param name="timestamp">The timestamp in seconds.</param>
        /// <returns>The index of the Feature that is closest to the timestamp, including the Feature's length.</returns>
        public override int GetIntersectingIndex(float timestamp)
        {
            lock (_lock)
            {
                int index = GetCacheIndex(timestamp);

                if (index == cachedTimestamps.Count)
                    return _features.Count;

                int cached = cachedIndices[cachedTimestamps[index]];

                for (int i = cached; i < _features.Count; i++)
                {
                    T feature = _features[i];

                    if (feature.timestamp + feature.length > timestamp)
                        return i;
                }

                return _features.Count;
            }
        }

        private int BinarySearch(float timestamp)
        {
            int min = 0;
            int max = _features.Count - 1;

            while (min <= max)
            {
                int mid = min + (max - min >> 1);
                int comp = _features[mid].timestamp.CompareTo(timestamp);

                if (comp == 0)
                    return mid;

                if (comp < 0)
                    min = mid + 1;
                else
                    max = mid - 1;
            }

            return ~min;
        }

        private int GetCacheIndex(float timestamp)
        {
            int time = Mathf.FloorToInt(timestamp / 5) * 5;

            int index = cachedTimestamps.BinarySearch(time);

            if (index < 0)
            {
                index = ~index;

                int cached = 0;

                if (index > 0)
                    cached = cachedIndices[cachedTimestamps[index - 1]];

                for (int i = cached; i < _features.Count; i++)
                {
                    T feature = _features[i];

                    if (feature.timestamp + feature.length > time)
                    {
                        cachedTimestamps.Insert(index, time);
                        cachedIndices.Add(time, i);
                        return index;
                    }
                }
            }

            return index;
        }

        private void ClearCache(float timestamp)
        {
            int index = GetCacheIndex(timestamp);

            for (int i = index; i < cachedTimestamps.Count; i++)
                cachedIndices.Remove(cachedTimestamps[i]);

            cachedTimestamps.RemoveRange(index, cachedTimestamps.Count - index);
        }

        /// <summary>
        /// Create a track of type T with a name.
        /// </summary>
        /// <param name="name">The name of the Track</param>
        /// <returns>The new Track.</returns>
        public static Track<T> Create(string name)
        {
            if (concreteType == null)
            {
                Debug.LogWarning("No Track found for " + typeof(T).Name);
                return null;
            }

            Track<T> track = CreateInstance(concreteType) as Track<T>;

            track.hideFlags = HideFlags.HideInHierarchy;

            track.name = name;

            return track;
        }
    }
}
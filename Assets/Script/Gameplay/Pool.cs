﻿using System.Collections.Generic;
using UnityEngine;

namespace YARG.Gameplay
{
    public interface IPoolable
    {
        public Pool ParentPool { get; set; }

        public void EnableFromPool();
        public void DisableIntoPool();
    }

    public class Pool : MonoBehaviour
    {
        [SerializeField]
        private GameObject _prefab;
        [SerializeField]
        private int _prewarmAmount = 15;
        [SerializeField]
        private int _objectCap = 500;

        private readonly Stack<IPoolable> _pooled = new();
        private int _spawnedCount;
        private int TotalCount => _pooled.Count + _spawnedCount;

        protected virtual void Awake()
        {
            for (int i = 0; i < _prewarmAmount; i++)
            {
                _pooled.Push(CreateNew());
            }
        }

        private IPoolable CreateNew()
        {
            if (TotalCount + 1 > _objectCap)
            {
                return null;
            }

            var gameObject = Instantiate(_prefab, transform);
            gameObject.SetActive(false);

            var poolable = gameObject.GetComponent<IPoolable>();
            poolable.ParentPool = this;

            return poolable;
        }

        public bool CanSpawnAmount(int count)
        {
            if (TotalCount + count <= _objectCap)
            {
                return true;
            }

            if (_pooled.Count >= count)
            {
                return true;
            }

            return false;
        }

        public IPoolable TakeWithoutEnabling()
        {
            if (_pooled.TryPop(out var poolable))
            {
                _spawnedCount++;
                return poolable;
            }

            poolable = CreateNew();
            if (poolable != null)
            {
                _spawnedCount++;
            }

            return poolable;
        }

        public IPoolable Take()
        {
            var poolable = TakeWithoutEnabling();
            if (poolable == null)
            {
                return null;
            }

            poolable.EnableFromPool();
            return poolable;
        }

        public void Return(IPoolable poolable)
        {
            // Skip if the stack already contains this poolable
            if (_pooled.Contains(poolable)) return;

            _spawnedCount--;

            poolable.DisableIntoPool();
            _pooled.Push(poolable);

            OnReturned(poolable);
        }

        protected virtual void OnReturned(IPoolable poolable)
        {

        }
    }
}
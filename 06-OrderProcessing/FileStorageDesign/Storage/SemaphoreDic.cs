using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace FileStorageDesign.Storage
{
    internal class SemaphoreDic
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();
        
        public SemaphoreSlim GetFileLock(string fileName)
        {
            return _fileLocks.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1));
        }

    }
}

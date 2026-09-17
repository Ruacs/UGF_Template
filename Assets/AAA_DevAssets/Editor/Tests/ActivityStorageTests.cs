using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace Lokas.Editor.Tests
{
    public sealed class ActivityStorageTests
    {
        private sealed class Backend : IActivityStorageBackend
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool SaveSucceeded = true;
            public bool ReadFails;
            public string Read(string key)
            {
                if (ReadFails) throw new IOException("read failed");
                return Values.TryGetValue(key, out string value) ? value : null;
            }
            public void Write(string key, string value) { Values[key] = value; }
            public bool Flush() => SaveSucceeded;
        }

        [Test]
        public void ProfileModuleAndLocalKeyAreIsolatedEvenWithSeparators()
        {
            var backend = new Backend();
            var one = new ActivityStorage(backend, "account/one", "race");
            var two = new ActivityStorage(backend, "account", "one/race");
            var three = new ActivityStorage(backend, "account/one", "pass");
            one.WriteAsync("a/b", "race-state", 3).GetAwaiter().GetResult();
            two.WriteAsync("a/b", "other-profile", 1).GetAwaiter().GetResult();
            three.WriteAsync("a/b", "pass-state", 2).GetAwaiter().GetResult();
            Assert.That(backend.Values.Count, Is.EqualTo(3));
            ActivityStorageReadResult result = one.ReadAsync("a/b").GetAwaiter().GetResult();
            Assert.That(result.Status, Is.EqualTo(ActivityStorageReadStatus.Found));
            Assert.That(result.Payload, Is.EqualTo("race-state"));
            Assert.That(result.SchemaVersion, Is.EqualTo(3));
            Assert.That(two.ReadAsync("a/b").GetAwaiter().GetResult().Payload, Is.EqualTo("other-profile"));
            Assert.That(three.ReadAsync("a/b").GetAwaiter().GetResult().Payload, Is.EqualTo("pass-state"));
        }

        [Test]
        public void MissingCorruptAndBackendFailureHaveDifferentResults()
        {
            var backend = new Backend();
            var storage = new ActivityStorage(backend, "profile", "race");
            Assert.That(storage.ReadAsync("state").GetAwaiter().GetResult().Status, Is.EqualTo(ActivityStorageReadStatus.Missing));
            storage.WriteAsync("state", "", 1).GetAwaiter().GetResult();
            string key = new List<string>(backend.Values.Keys)[0];
            backend.Values[key] = "{}";
            Assert.That(storage.ReadAsync("state").GetAwaiter().GetResult().Status, Is.EqualTo(ActivityStorageReadStatus.Corrupt));
            backend.Values[key] = "{broken";
            Assert.That(storage.ReadAsync("state").GetAwaiter().GetResult().Status, Is.EqualTo(ActivityStorageReadStatus.Corrupt));
            backend.ReadFails = true;
            Assert.Throws<IOException>(() => storage.ReadAsync("state").GetAwaiter().GetResult());
        }

        [Test]
        public void SaveFailureIsObservableAndSameWriteCanBeRetried()
        {
            var backend = new Backend { SaveSucceeded = false };
            var storage = new ActivityStorage(backend, "profile", "race");
            Assert.Throws<IOException>(() => storage.WriteAsync("state", "pending", 1).GetAwaiter().GetResult());
            Assert.Throws<IOException>(() => storage.FlushAsync().GetAwaiter().GetResult());
            backend.SaveSucceeded = true;
            storage.WriteAsync("state", "pending", 1).GetAwaiter().GetResult();
            Assert.That(backend.Values.Count, Is.EqualTo(1));
        }

        [Test]
        public void CancellationBeforeWriteLeavesBackendUntouched()
        {
            var backend = new Backend();
            var storage = new ActivityStorage(backend, "profile", "race");
            Assert.Throws<OperationCanceledException>(() => storage.WriteAsync("state", "pending", 1, new CancellationToken(true)).GetAwaiter().GetResult());
            Assert.That(backend.Values, Is.Empty);
        }
    }
}

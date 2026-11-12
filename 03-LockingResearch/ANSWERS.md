
# Assignment 03 — Locking Research

Student name:  Hossam Eldubai
Group / ID:    


## Q1. What is System.Threading.Monitor used for?

`Monitor` is used to control access to shared resources between multiple threads.

It allows only one thread at a time to enter a critical section, which helps prevent race conditions when multiple threads try to access the same data.

## Q2. What does the C# lock statement compile to?

The `lock` statement uses `Monitor.Enter()` and `Monitor.Exit()` internally.

It also uses a `try/finally` block to make sure the lock is released even if an exception happens.

## Q3. List at least two differences between using lock and calling Monitor APIs directly.

1. `lock` is easier to write and read, while using `Monitor` directly requires more code.
2. `lock` automatically releases the lock using `finally`, while with `Monitor.Enter()` and `Monitor.Exit()` we need to handle this manually.
3. `Monitor` provides extra methods like `TryEnter()`, `Wait()`, and `Pulse()`, which are not available with a normal `lock` statement.

## Q4. When might a developer use Monitor.TryEnter, Monitor.Pulse, or Monitor.Wait instead of a plain lock?

- `TryEnter`: When we want to try acquiring a lock without waiting forever if another thread is holding it. We can also specify a timeout.
- `Wait`: When a thread needs to wait for a specific condition and release the lock temporarily.
- `Pulse`: When a thread wants to notify another waiting thread that something has changed and it can continue.

These methods are useful when we need more control over thread synchronization.

## Q5. Write the same critical section twice: once with lock, once with Monitor.Enter / Monitor.Exit.

**Using lock:**

```csharp
private readonly object _locker = new();

public void Increment()
{
    lock (_locker)
    {
        counter++;
    }
}
```

**Using Monitor:**

```csharp
private readonly object _locker = new();

public void Increment()
{
    Monitor.Enter(_locker);

    try
    {
        counter++;
    }
    finally
    {
        Monitor.Exit(_locker);
    }
}
```

Both examples protect the same critical section and prevent multiple threads from updating the counter at the same time.

## References

- <https://dev.to/rasulhsn/introduction-to-monitor-class-in-c-422>
- <https://medium.com/jit-team/multithreaded-work-synchronization-in-c-net-part-1-480e0dec999c>

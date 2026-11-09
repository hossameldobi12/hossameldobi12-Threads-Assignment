## Phase 1 Questions

### 1. Why are the sync and async times almost the same when orders are awaited one after another?

Because we process orders one by one in both cases. We use `await` to wait for each order to finish before processing the next one, so we are not processing multiple orders at the same time. Async does not always mean faster execution, but it helps avoid blocking threads while waiting.

### 2. Is the thread ID before an await always the same as after it? Why? Use your own log lines as evidence.

No, the thread ID is not always the same. After an `await`, the method may continue on a different thread from the Thread Pool.

In my logs, I saw different thread IDs, such as 8, 12, and 13, during the async execution.

### 3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?

With `Task.Delay()`, the thread is free to do other work while waiting.

With `Thread.Sleep()`, the current thread is blocked and cannot do other work until the sleep time finishes.

### 4. Why should a repository method return Task<T> instead of T?

Because repository methods may perform I/O operations, such as reading or writing files or accessing a database. Returning `Task<T>` allows us to use `await` without blocking the current thread while waiting for the operation to finish.

### 5. What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?

`File.ReadAllText()` is synchronous. It blocks the current thread until the file is fully read.

`File.ReadAllTextAsync()` is asynchronous. It allows the current thread to do other work while waiting for the file reading operation. Normally, it does not need to keep a thread occupied during the disk I/O.




# Phase 2 — Questions & Answers

### 1. What is the difference between starting a task and awaiting it?

Starting a task means the operation begins executing and returns a Task that represents its work.

Awaiting a task means waiting asynchronously until it finishes before continuing to the next step.

We can start multiple tasks first, then use `Task.WhenAll()` to wait for all of them. This allows independent operations to run concurrently.

### 2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?

No, creating a Task does not always create a new thread.

A Task represents an asynchronous operation. It may use a Thread Pool thread, or it may represent I/O work without occupying a thread while waiting.

`Task.Delay()` does not block or occupy a thread while waiting. It completes after the specified time.

### 3. What happens if you call .Result or .Wait() on an unfinished task instead of await? Why is it dangerous?

`.Result` and `.Wait()` block the current thread until the task finishes.

This can reduce application performance because threads cannot do other work while blocked. It can also cause deadlocks in environments with a SynchronizationContext, such as UI applications.

`await` is preferred because it waits asynchronously without blocking the current thread.

### 4. What happens to an exception thrown inside a task that is never awaited?

The task stores the exception and its status becomes `Faulted`.

If nobody awaits the task or checks its exception, the error may go unnoticed by the application, and the expected operation may never complete successfully.

We should observe tasks and handle their exceptions.

### 5. When should you use Task.Run(), and when should you avoid it?

Use `Task.Run()` for CPU-bound work that takes time, such as calculations or processing a large amount of data.

Avoid wrapping I/O-bound asynchronous operations in `Task.Run()`, such as reading files or calling APIs. These operations already have asynchronous APIs and do not need an extra Thread Pool thread.

In this project, I used `Task.Run()` for the CPU-intensive invoice checksum calculation.

### 6. What is the difference between Task and Task<T>?

`Task` represents an asynchronous operation that does not return a result.

`Task<T>` represents an asynchronous operation that returns a result of type `T`.

For example, `Task` can represent saving an order, while `Task<bool>` can represent processing an order and returning whether it succeeded.

### 7. Why can a lock block not contain an await?

The `lock` statement uses a thread-affine synchronization mechanism. The thread that enters the lock must be the same thread that exits it.

An `await` may resume on a different thread, so C# does not allow `await` inside a `lock` block.

For asynchronous synchronization, we can use `SemaphoreSlim` with `WaitAsync()`.

### 8. Why did the unprotected stock reservation oversell, and how did you prevent it?

The unsafe reservation had a race condition.

Two orders could read the same stock quantity before either one updated the file. Both orders then believed the product was available and both reserved it.

I prevented this by using `SemaphoreSlim` through `JsonFileStore.UpdateAsync()`. It protects the complete read-modify-write operation for the same file.

This ensures that only one reservation updates the stock at a time, preventing overselling.
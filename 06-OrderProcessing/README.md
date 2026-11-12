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




# Phase 3 — Full Checkout Workflow

## Task 9.6 — Sync vs Async Comparison

### 1. Does async make one order's workflow faster?

No. Async does not necessarily make a single order finish faster.

Both versions still perform the same operations and simulated delays. The main benefit of async is that it allows the application to handle multiple operations without blocking threads while waiting for I/O operations.

### 2. Why does async scale better for many orders?

Async uses threads more efficiently.

When an async operation is waiting for I/O, such as reading a file or calling an external API, its thread is not blocked. The thread can be used for other work.

In the sync version, each order runs on a dedicated thread, and that thread stays blocked during `Thread.Sleep` and other synchronous operations.

This makes async more efficient when processing many orders concurrently.

### 3. What happens to the sync version with 1,000 orders?

The sync version would need to create and manage 1,000 dedicated threads if every order follows the same approach.

This causes high memory usage and thread-management overhead. It can also lead to thread exhaustion and poor scalability.

The async version can handle many concurrent orders without requiring one dedicated thread for every order.

### 4. Why is async essential for apps that call databases, files, and APIs?

These operations usually involve waiting for external resources.

Using async methods allows the application to wait without blocking a thread. This improves resource usage, scalability, and responsiveness, especially when many requests are running at the same time.

---

## Task 9.7 — Fire-and-Forget Investigation

### 1. What is the problem in the original code?

The problem is that `SendConfirmationAsync` is called without `await`.

This means the email operation starts, but `ProcessOrderAsync` does not wait for it to finish.

As a result, the method can return before the email is sent, and exceptions from the email operation are not propagated to the caller.

### 2. What happens if the email is still being sent when the method returns or the console app exits?

The method continues executing after starting the email operation.

The following output demonstrates that `ProcessOrderAsync` returns before the email operation finishes:

**Console Output Before the Fix:**

```text
Starting order processing...
OrderRepository -> Order saved: 1
EmailService -> Sending email for Order #1, Attempt #2
EmailService -> Rendering email template...
Order processing finished
Runner continues after ProcessOrderAsync returns.
```

The email operation had not finished when the method returned.

If the console application exits before the email operation completes, the process may terminate before the email is sent successfully.

### 3. What happens if the email operation throws an exception?

Because the returned Task is not awaited, the exception is stored in the faulted Task instead of being propagated to the caller through `ProcessOrderAsync`.

Therefore, a `try-catch` around the call to `ProcessOrderAsync` cannot catch that email exception.

The failure may remain unobserved, and the caller may continue as if the operation had completed successfully.

### 4. Fixed Implementation

The fix is to use `await` when calling `SendConfirmationAsync`.

```csharp
public async Task ProcessOrderAsync(Order order, Customer customer)
{
    await _orderRepository.SaveOrderAsync(order);

    await _emailService.SendConfirmationAsync(
        order,
        customer,
        CancellationToken.None);

    Console.WriteLine("Order processing finished");
}
```

Now, `ProcessOrderAsync` waits until the email operation completes. If sending the email fails, the exception propagates to the caller.

**Console Output After the Fix:**

The following is a representative successful execution:

```text
Starting order processing...
OrderRepository -> Order saved: 1
EmailService -> Sending email for Order #1, Attempt #1
EmailService -> Rendering email template...
EmailService -> Connecting to SMTP server...
EmailService -> Sending confirmation email...
EmailService -> Confirmation email sent for Order #1
Order processing finished
Runner continues after ProcessOrderAsync returns.
```

Unlike the original version, the method prints `Order processing finished` only after the email operation completes successfully.

### 5. When is await necessary?

`await` is necessary when:

- The next operation depends on the result of the current operation.
- The caller must wait for the operation to finish.
- Exceptions must be propagated and handled by the caller.
- The application must ensure that the operation finishes before exiting or disposing required resources.

### 6. When is fire-and-forget acceptable?

Fire-and-forget can be acceptable when the operation is intentionally independent of the caller and its completion is not required before returning.

However, failures must still be handled, and the operation's lifetime must be managed.

For important background work, such as sending order confirmation emails, a background queue or durable message system is safer than starting an untracked Task.

### 7. Why is async void dangerous?

`async void` does not return a Task.

This means the caller cannot:

- Await the operation.
- Know when it finishes.
- Receive its result.
- Catch its exceptions through a normal `try-catch` around the method call.

This makes error handling and tracking the operation much harder.

### 8. Why is async void acceptable for event handlers?

Some event handlers require a `void` return type because of the event delegate signature.

For example, a UI button click handler may need to be `async void`.

In this case, `async void` is acceptable because the event framework expects a void-returning handler.

However, exceptions should be handled inside the event handler using `try-catch`.




# Phase 4 — Questions & Answers

## 1. For each of Tasks 10.1–10.4: which tool did you use, and why can't "start, then await one by one" meet the requirement?

- **Task 10.1 — `Task.WhenEach`:** I used `Task.WhenEach` to process orders as soon as each task completes. Starting all tasks and then awaiting them one by one does not process results in completion order. It waits for the first task in the list, even if other tasks have already finished.

- **Task 10.2 — `Task.WhenAll`:** I used `Task.WhenAll` to run the four dashboard loads concurrently and wait until all of them finish. Awaiting tasks one by one would make the loads run sequentially if each task is started only when awaited.

- **Task 10.3 — `Task.WhenAny`:** I used `Task.WhenAny` to get the first shipping carrier that returns a successful quote. Awaiting carriers one by one would wait for slower carriers even when a faster quote is already available.

- **Task 10.4 A — `Parallel.ForEach`:** I used `Parallel.ForEach` to distribute fraud-scoring work across multiple CPU cores. Starting tasks and awaiting them one by one would not provide parallel CPU execution.

- **Task 10.4 B — `SemaphoreSlim` with `Task.WhenAll`:** I used `SemaphoreSlim` to limit concurrent email sends to four. `Task.WhenAll` waits for all email tasks to finish. Awaiting them one by one would send emails sequentially instead of allowing concurrent I/O operations.

## 2. In 10.2, when several loads fail, how do you get every exception? What does a plain await give you?

I used `Task.WhenAll` to wait for all four loads. If multiple tasks fail, I can catch the exception and inspect the returned task's `Exception` property, then use `Flatten().InnerExceptions` to get every exception.

A plain `await` on the `Task.WhenAll` task throws one exception, not all the exceptions. The other exceptions are still available through the task's `Exception` property.

## 3. In 10.3, what happens to the losing carriers if you never cancel them?

The losing carriers continue running even after the fastest carrier returns a quote. Their tasks may still consume resources and perform unnecessary work.

To stop them, I can use a `CancellationTokenSource` and cancel the losing tasks after getting the successful result. This only works if the carriers support and respect cancellation.

## 4. Why does 10.4 A get faster with more CPU cores, while 10.4 B does not depend on the number of cores?

**10.4 A — Fraud Scoring:**

This is CPU-bound work. It performs calculations that need CPU processing. More CPU cores allow multiple parts of the work to run in parallel, which can reduce execution time.

**10.4 B — Email Re-sending:**

This is I/O-bound work. Most of the time is spent waiting for simulated email operations (`Task.Delay`), not performing CPU calculations. Multiple emails can be in progress while waiting, so the main limit is the concurrency limit set by `SemaphoreSlim`, not the number of CPU cores.

## 5. What is the difference between parallel work (10.4 A) and concurrent asynchronous work (10.1)?

- **Parallel work:** Multiple operations execute at the same time on different CPU cores. It is useful for CPU-bound work, such as fraud scoring.
- **Concurrent asynchronous work:** Multiple operations are in progress at the same time, but they do not necessarily execute simultaneously. While one operation is waiting for I/O, another can make progress. It is useful for operations such as processing orders asynchronously.

In short, parallelism is about using multiple CPU cores, while concurrency is about managing multiple operations that are in progress.

## 6. Show a cancelled run (`--cancel-after`) and explain what happened to the orders, the stock and the payments.

I used the `--cancel-after` argument to test cancellation during order processing.

**Command:**

```bash
dotnet run -- phase4 --cancel-after 2000
```

When the cancellation token is triggered, cancellation is passed to the running operations. The program catches `OperationCanceledException` and reports that the operation was cancelled.

The final state depends on when cancellation happens during order processing:

- **Orders:** Orders already saved before cancellation remain saved. Orders that did not finish processing should not be saved as completed.
- **Stock:** Stock reserved by an order that gets cancelled should be released.
- **Payments:** Payments already completed before cancellation remain recorded. An operation cancelled before payment should not create a successful payment.

I would verify the actual result by checking `orders.json`, `products.json`, and `payments.json` after the cancelled run.
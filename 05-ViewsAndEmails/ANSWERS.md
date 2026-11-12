# Assignment 05 — Answers

Student name: Hossam Eldubai  
Group / ID:  Saturday & Tuesday 2pm

## Failures you observed

When I ran the project, the actual view count was much lower than expected. Sometimes, I also got a `KeyNotFoundException`.

The email jobs had a similar problem. Some jobs were missing, and the number of sent emails was not always correct.

This happened because multiple threads were accessing and modifying the same collections at the same time.

## What you changed

For the view counting, I replaced `Dictionary` with `ConcurrentDictionary` and used `AddOrUpdate` to update the count safely.

For the email jobs, I replaced `Queue` with `ConcurrentQueue` and used `TryDequeue` to let workers take jobs safely.

I also kept the `lock` around `SentEmails.Add(job)` because multiple workers can add to the same List.

After these changes, I got the correct results every time I tested the project.

## Collection safety vs business-rule safety

Using a thread-safe collection does not mean the whole code is automatically safe.

For example, `ConcurrentQueue` makes adding and removing jobs safe, but we still need to make sure the workers don't stop while there are jobs waiting to be processed.

That's why I used `Join()` to wait for all producers to finish before setting `ProducersDone` to `true`.

The main thing I learned is that we need to think about the whole operation, not just whether the collection itself is thread-safe.
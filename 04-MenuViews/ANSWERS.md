# Assignment 04 — Answers

Student name: Hossam Eldubai  
Group / ID:  Saturday & Tuesday 2pm

## Why Expected and Actual disagree

The problem was a **Race Condition**.

Multiple threads were updating the same `Views` field at the same time.

The `Views++` operation is not atomic. It involves reading the current value, adding one, and writing the result back.

Two threads could read the same value and overwrite each other's updates. This caused some increments to be lost, so the Actual value was lower than the Expected value.

## What you changed

I replaced `Views++` with `Interlocked.Increment(ref Views)`.

`Interlocked.Increment` performs the increment as an atomic operation. This makes it safe for multiple threads to update the same counter without losing increments.

After the change, the Expected and Actual values matched:

- Expected: 400000
- Actual: 400000

## When would you protect the update differently

I would use `lock` when an update involves multiple related operations that must happen together.

For example, in a table booking system, I need to check if a table is available, decrease `TablesLeft`, and increase `Booked`.

These operations should be protected together to prevent other threads from changing the shared data in the middle of the process.

`Interlocked` is a good choice for a single atomic operation, while `lock` is more suitable for protecting a group of related operations.
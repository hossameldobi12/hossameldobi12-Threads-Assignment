# Assignment 02 — Answers

Student name: Hossam Eldubai  
Group / ID:  Saturday & Tuesday 2pm

## What is wrong

The problem is a Race Condition.

All customers use the same booking object, and multiple threads can access it at the same time.

The problem happens because a thread checks if there are available tables, but before it updates the number, another thread can do the same check.

This happens because of the `Thread.Sleep(20)`, which gives other threads a chance to run before the first thread finishes booking.

Also, `TablesLeft--` and `Booked++` can be accessed by multiple threads at the same time, which leads to incorrect values.

That's why sometimes more customers get booked than the available tables, and `TablesLeft` can become negative.

### Results before the fix

| Run | Booked | Tables Left |
|---|---:|---:|
| 1 | 9 | -4 |
| 2 | 12 | -7 |
| 3 | 7 | -2 |
| 4 | 9 | -4 |
| 5 | 5 | 0 |

## How you fixed it

I fixed the problem using `lock`.

I created a private lock object and used it around the whole booking operation, starting from checking if a table is available until updating the booking numbers.

This means only one thread can book a table at a time. Other threads have to wait until the current thread finishes.

This prevents multiple threads from booking the same available tables and keeps the numbers correct.

The final result should always be:

- Booked = 5
- TablesLeft = 0
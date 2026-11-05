# Part 01 — Invoice Export

## R1 — Run the Original Code

I ran the original application and it generated all five invoice files successfully.

**Execution time: 6.0 seconds**

## R2 — What Were the Problems?

I noticed a few problems in the original code:

- It used `Thread.Sleep()`, which blocks the current thread.
- It used synchronous file operations like `File.ReadAllText()` and `File.WriteAllText()`.
- It processed orders one by one, waiting for each order to finish before starting the next one.

This made the export process slower than it needed to be.

## R3 — What Did I Change?

I updated the code to use `async` and `await`.

- Replaced `Thread.Sleep()` with `Task.Delay()`.
- Used `File.ReadAllTextAsync()` to read customer files.
- Used `File.WriteAllTextAsync()` to write invoice files.
- Changed the methods to return `Task` or `Task<T>` and added the `Async` suffix to their names.
- Started processing all orders before awaiting their tasks one by one.

I didn't use `Task.WhenAll()` because it's not allowed in this part of the assignment.

Each order still follows the same steps:

1. Get the order.
2. Read the customer profile.
3. Write the invoice.

## R4 — Before and After

**Before: 6.0 seconds**

```text
Wrote invoice-1001.txt
Wrote invoice-1002.txt
Wrote invoice-1003.txt
Wrote invoice-1004.txt
Wrote invoice-1005.txt
Export finished in 6.0 s
```

**After: 1.9 seconds**

```text
Wrote invoice-1003.txt
Wrote invoice-1005.txt
Wrote invoice-1004.txt
Wrote invoice-1001.txt
Wrote invoice-1002.txt
Export finished in 1.9 s
```

### Result

The execution time went from **6.0 seconds to 1.9 seconds**.

All five invoices were generated, and I checked that their content is still the same as before.

The order of the output changed because orders are now processed concurrently, which is expected.

**In the end, I improved the performance without changing the original business flow or invoice content.**
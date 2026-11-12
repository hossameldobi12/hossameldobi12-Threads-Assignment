
using System.Diagnostics;
using FileStorageDesign.Exceptions;
using FileStorageDesign.Interfaces;
using FileStorageDesign.Models;

namespace FileStorageDesign.Services;

public class ShippingQuoteService
{
    private readonly List<IShippingCarrier> _carriers;

    private static readonly TimeSpan TimeBudget =
        TimeSpan.FromMilliseconds(1200);

    public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
    {
        _carriers = carriers.ToList();
    }

    public async Task<ShippingQuote> GetFastestQuoteAsync(
        Order order,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        using var cts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        var carrierTasks = _carriers.ToDictionary(
            carrier => carrier.Name,
            carrier => carrier.GetQuoteAsync(order, cts.Token));

        var pendingTasks = carrierTasks.Values.ToList();

        try
        {
            while (pendingTasks.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                var remainingTime = TimeBudget - stopwatch.Elapsed;

                if (remainingTime <= TimeSpan.Zero)
                    break;

                using var timeoutCts = new CancellationTokenSource();

                var timeoutTask = Task.Delay(
                    remainingTime,
                    timeoutCts.Token);

                var completedTask = await Task.WhenAny(
                    pendingTasks.Cast<Task>().Append(timeoutTask));

                ct.ThrowIfCancellationRequested();

                if (completedTask == timeoutTask)
                    break;

                timeoutCts.Cancel();

                var completedCarrier = carrierTasks.First(
                    pair => pair.Value == completedTask);

                pendingTasks.Remove(completedCarrier.Value);

                try
                {
                    var price = await completedCarrier.Value;

                    // Capture the winning time before cancellation cleanup.
                    stopwatch.Stop();
                    var elapsedMs = stopwatch.ElapsedMilliseconds;

                    // These are the carriers still running when the quote wins.
                    var canceledCarriers = carrierTasks
                        .Where(pair => pendingTasks.Contains(pair.Value))
                        .Select(pair => pair.Key)
                        .ToList();

                    // Cancel all remaining carriers.
                    cts.Cancel();

                    await ObserveRemainingTasksAsync(pendingTasks);

                    return new ShippingQuote
                    {
                        Carrier = completedCarrier.Key,
                        Price = price,
                        ElapsedMs = elapsedMs,
                        CancelledCarriers = canceledCarriers
                    };
                }
                catch (ShippingProviderUnavailableException)
                {
                    // This carrier failed. Continue waiting for others.
                }
            }

            ct.ThrowIfCancellationRequested();

            if (pendingTasks.Count == 0)
            {
                throw new ShippingProviderUnavailableException(
                    "All carriers failed");
            }

            // Time budget expired: collect carriers still running.
            stopwatch.Stop();
            var totalElapsedMs = stopwatch.ElapsedMilliseconds;

            var cancelledCarriers = carrierTasks
                .Where(pair => pendingTasks.Contains(pair.Value))
                .Select(pair => pair.Key)
                .ToList();

            cts.Cancel();

            await ObserveRemainingTasksAsync(pendingTasks);

            return new ShippingQuote
            {
                Carrier = "Flat rate",
                Price = 25.00m,
                ElapsedMs = totalElapsedMs,
                CancelledCarriers = cancelledCarriers
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            cts.Cancel();

            await ObserveRemainingTasksAsync(pendingTasks);

            throw;
        }
        catch
        {
            cts.Cancel();

            await ObserveRemainingTasksAsync(pendingTasks);

            throw;
        }
    }

    private static async Task ObserveRemainingTasksAsync(
        IEnumerable<Task<decimal>> tasks)
    {
        foreach (var task in tasks)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected after cancellation.
            }
            catch (ShippingProviderUnavailableException)
            {
                // Expected carrier failure.
            }
        }
    }
}

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Samples.Integration;

/// <summary>
/// Marks a test method as retryable. The test will be run up to the specified number of times
/// if it fails, until it passes or the retry limit is exhausted.
///
/// Usage:
///   [RetryFact(maxRetries: 3)]
///   public void MyFlakyTest() { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RetryFactAttribute : FactAttribute
{
    /// <summary>
    /// The maximum number of times to retry the test.
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="RetryFactAttribute"/> class.
    /// </summary>
    /// <param name="maxRetries">The maximum number of times to retry the test. Default is 3.</param>
    public RetryFactAttribute(int maxRetries = 3)
    {
        if (maxRetries < 1)
            throw new ArgumentException("maxRetries must be at least 1", nameof(maxRetries));

        MaxRetries = maxRetries;
    }
}

/// <summary>
/// Helper for retrying test logic.
///
/// Usage in tests:
///   [RetryFact(maxRetries: 3)]
///   public void MyFlakyTest() => RetryHelper.Execute(ActualTestLogic, maxRetries: 3);
///
///   private void ActualTestLogic()
///   {
///       // test code here
///   }
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Executes the provided action, retrying on failure up to maxRetries times.
    /// </summary>
    public static void Execute(Action action, int maxRetries = 3)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (maxRetries < 1)
            throw new ArgumentException("maxRetries must be at least 1", nameof(maxRetries));

        var exceptions = new List<Exception>();

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                action();
                return; // Success
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);

                if (attempt < maxRetries - 1)
                {
                    System.Threading.Thread.Sleep(100); // Small delay before retry
                }
            }
        }

        // All retries failed
        if (exceptions.Count == 1)
        {
            throw exceptions[0];
        }

        throw new AggregateException(
            $"Test failed after {maxRetries} attempts",
            exceptions);
    }

    /// <summary>
    /// Executes the provided async action, retrying on failure up to maxRetries times.
    /// </summary>
    public static async Task ExecuteAsync(Func<Task> action, int maxRetries = 3)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (maxRetries < 1)
            throw new ArgumentException("maxRetries must be at least 1", nameof(maxRetries));

        var exceptions = new List<Exception>();

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await action();
                return; // Success
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);

                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(100); // Small delay before retry
                }
            }
        }

        // All retries failed
        if (exceptions.Count == 1)
        {
            throw exceptions[0];
        }

        throw new AggregateException(
            $"Test failed after {maxRetries} attempts",
            exceptions);
    }
}

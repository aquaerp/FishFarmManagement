using Xunit;

// CultureInfo and WinForms direction are process-wide state. Running localization
// tests concurrently would make otherwise valid assertions nondeterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

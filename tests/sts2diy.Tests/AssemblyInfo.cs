using Xunit;

// MonoMod's shared IL proxy initialization must not race between test classes.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

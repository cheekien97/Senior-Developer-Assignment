using Xunit;

// The integration tests each spin up a WebApplicationFactory host. Host
// construction relies on HostFactoryResolver's process-wide diagnostic
// listener, which is not safe to drive from multiple threads at once. Running
// the test classes sequentially avoids a race that can otherwise surface as
// "The entry point exited without ever building an IHost".
[assembly: CollectionBehavior(DisableTestParallelization = true)]

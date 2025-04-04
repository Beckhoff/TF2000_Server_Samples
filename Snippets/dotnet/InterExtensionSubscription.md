# Inter-extension subscription

Extensions can execute API requests to other server extensions or to the HMI server.
This is demonstrated in the *InterExtensionCommunication*
([.NET](../../Extensions/dotnet/InterExtensionCommunication/README.md)) sample.

Subscriptions between extensions are currently not supported by the .NET extension API.
You can implement a polling mechanism like this:

**Usage (e.g. in your extension's `Init` function):**

```cs
var monitor = new ValueMonitor(async () =>
{
    var command = new Command("Diagnostics::SERVERTIME");
    var (_, _, resultCommand) = await TcHmiApplication.AsyncHost.ExecuteAsync(TcHmiApplication.Context, command);
    return resultCommand;
}, TimeSpan.FromSeconds(5));
monitor.OnChange += newCommandResult => { /* handle subscription changes */ };
monitor.Start();
```

**Implementation of the helper class:**

```cs
using System;
using System.Threading;
using System.Threading.Tasks;

public class ValueMonitor
{
    private readonly Func<Task<TcHmiSrv.Core.Command>> _executeCommand;
    private readonly TimeSpan _pollingInterval;
    private CancellationTokenSource _cts;

    public event Action<TcHmiSrv.Core.Command> OnChange;

    public ValueMonitor(Func<Task<TcHmiSrv.Core.Command>> executeCommand, TimeSpan pollingInterval)
    {
        _executeCommand = executeCommand ?? throw new ArgumentNullException(nameof(executeCommand));
        _pollingInterval = pollingInterval;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = MonitorAsync(_cts.Token);
    }

    public void Stop() => _cts.Cancel();

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        TcHmiSrv.Core.Command previousResult = new();

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await _executeCommand();

            if ((previousResult.ReadValue == null && result.ReadValue != null) ||
                !previousResult.ReadValue.Equals(result.ReadValue) ||
                previousResult.ExtensionResult != result.ExtensionResult ||
                previousResult.Result != result.Result)
            {
                previousResult = result.DeepCopy();
                OnChange?.Invoke(previousResult);
            }

            try
            {
                await Task.Delay(_pollingInterval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
```

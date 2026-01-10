# JsonRpc
[![Unit Tests](https://github.com/juliusbaechle/CsJsonRpc/actions/workflows/unit_tests.yml/badge.svg)](https://github.com/juliusbaechle/CsJsonRpc/actions/workflows/unit_tests.yml)
[![CodeQL](https://github.com/juliusbaechle/CsJsonRpc/actions/workflows/codeql.yml/badge.svg)](https://github.com/juliusbaechle/CsJsonRpc/actions/workflows/codeql.yml)

JSON-RPC Library supporting interfaces like

```csharp
public interface IReceptionClient : IDisposable
{
    public Task<int> AppendOrder(Order a_order);

    /// May throw OrderNotFoundException
    public void StartOrder(int a_id);

    /// May throw OrderNotFoundException
    public Task<Order> GetOrder(int a_id);

    public event Action<int, Order.EState> OrderStateChanged;
}
```

with
- JSON-RPC Notifications (```StartOrder```)
- Async JSON-RPC Requests (```AppendOrder``` and ```GetOrder```)
- Subscriptions (```OrderStateChanged```)
- Custom Exceptions (```OrderNotFoundException```)

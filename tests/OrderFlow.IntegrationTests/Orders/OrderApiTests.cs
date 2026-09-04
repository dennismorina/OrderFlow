using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrderFlow.Application.Orders.Models;
using OrderFlow.Domain.Orders;

namespace OrderFlow.IntegrationTests.Orders;

public sealed class OrderApiTests : IClassFixture<OrderFlowApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public OrderApiTests(OrderFlowApiFactory factory)
    {
        _client = factory.CreateClient();

        _jsonOptions = new JsonSerializerOptions(
            JsonSerializerDefaults.Web);

        _jsonOptions.Converters.Add(
            new JsonStringEnumConverter());
    }

    [Fact]
    public async Task Create_then_get_order_returns_created_order()
    {
        var request = CreateRequest("SHOP-INT-001");

        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<OrderDto>(_jsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(created);
        Assert.Equal(OrderStatus.Created, created.Status);

        var getResponse = await _client.GetAsync(
            $"/api/orders/{created.Id}", TestContext.Current.CancellationToken);

        getResponse.EnsureSuccessStatusCode();

        var loaded = await getResponse.Content
            .ReadFromJsonAsync<OrderDto>(_jsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded.Id);
    }

    [Fact]
    public async Task Repeating_same_external_order_is_idempotent()
    {
        var request = CreateRequest("SHOP-INT-002");

        var first = await _client.PostAsJsonAsync(
            "/api/orders",
            request, TestContext.Current.CancellationToken);

        var second = await _client.PostAsJsonAsync(
            "/api/orders",
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstOrder = await first.Content
            .ReadFromJsonAsync<OrderDto>(_jsonOptions, TestContext.Current.CancellationToken);

        var secondOrder = await second.Content
            .ReadFromJsonAsync<OrderDto>(_jsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(firstOrder!.Id, secondOrder!.Id);
    }

    [Fact]
    public async Task Workflow_reaches_completed_in_required_order()
    {
        var created = await CreateOrderAsync("SHOP-INT-003");

        await PostTransitionAsync(created.Id, "validate");
        await PostTransitionAsync(created.Id, "approve");
        await PostTransitionAsync(created.Id, "start-processing");

        var completed = await PostTransitionAsync(
            created.Id,
            "complete");

        Assert.Equal(OrderStatus.Completed, completed.Status);
        Assert.Equal(4, completed.History.Count);
    }

    [Fact]
    public async Task Processing_order_cannot_be_cancelled()
    {
        var created = await CreateOrderAsync("SHOP-INT-004");

        await PostTransitionAsync(created.Id, "validate");
        await PostTransitionAsync(created.Id, "approve");
        await PostTransitionAsync(created.Id, "start-processing");

        var response = await _client.PostAsJsonAsync(
            $"/api/orders/{created.Id}/cancel",
            new CancelOrderRequest("Customer request"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<OrderDto> CreateOrderAsync(string externalOrderId)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            CreateRequest(externalOrderId));

        response.EnsureSuccessStatusCode();

        return (await response.Content
            .ReadFromJsonAsync<OrderDto>(_jsonOptions))!;
    }

    private async Task<OrderDto> PostTransitionAsync(
        Guid orderId,
        string action)
    {
        var response = await _client.PostAsync(
            $"/api/orders/{orderId}/{action}",
            content: null);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"Transition '{action}' failed with {(int)response.StatusCode} " +
                $"{response.StatusCode}: {body}");
        }

        return (await response.Content
            .ReadFromJsonAsync<OrderDto>(_jsonOptions))!;
    }

    private static CreateOrderRequest CreateRequest(
        string externalOrderId)
        => new(
            externalOrderId,
            "C1000",
            "Integration Test Customer",
            [
                new CreateOrderItemRequest(
                    "SKU-100",
                    "Mechanical Keyboard",
                    2,
                    129.90m)
            ]);
}

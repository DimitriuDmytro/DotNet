namespace Platform.EventBus.Tests;

using System.Text;
using System.Text.Json;
using FluentAssertions;
using Platform.EventBus.Consumer;
using Platform.EventBus.Events;
using Platform.EventBus.Serialization;
using Platform.EventBus.Topology;
using Platform.EventBus.Tracing;
using RabbitMQ.Client;
using Xunit;

public class MessagingTests
{
    [Fact]
    public void JsonDefaults_SerializesInCamelCase_AndDeserializesCorrectly()
    {
        // Arrange
        var @event = new ProductPriceChangedEvent(
            productId: 42,
            productName: "Полірування кузова",
            oldPrice: 3500m,
            newPrice: 4200m,
            occurredAt: new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc),
            correlationId: "test-corr-123");

        // Act
        var json = JsonSerializer.Serialize(@event, JsonDefaults.Options);
        var deserialized = JsonSerializer.Deserialize<ProductPriceChangedEvent>(json, JsonDefaults.Options);

        // Assert
        json.Should().Contain("\"productId\":42");
        json.Should().Contain("\"oldPrice\":3500");
        json.Should().Contain("\"newPrice\":4200");
        json.Should().Contain("\"correlationId\":\"test-corr-123\"");

        deserialized.Should().NotBeNull();
        deserialized!.ProductId.Should().Be(42);
        deserialized.OldPrice.Should().Be(3500m);
        deserialized.NewPrice.Should().Be(4200m);
        deserialized.CorrelationId.Should().Be("test-corr-123");
    }

    [Fact]
    public void DeadLetterHelper_GetRetryCount_HandlesVariousHeaderTypes()
    {
        // 1. Null headers
        DeadLetterHelper.GetRetryCount(null).Should().Be(0);

        // 2. Missing key
        var emptyHeaders = new Dictionary<string, object?>();
        DeadLetterHelper.GetRetryCount(emptyHeaders).Should().Be(0);

        // 3. Integer
        var intHeaders = new Dictionary<string, object?> { [DeadLetterHelper.RetryHeaderName] = 2 };
        DeadLetterHelper.GetRetryCount(intHeaders).Should().Be(2);

        // 4. Long
        var longHeaders = new Dictionary<string, object?> { [DeadLetterHelper.RetryHeaderName] = 3L };
        DeadLetterHelper.GetRetryCount(longHeaders).Should().Be(3);

        // 5. String
        var strHeaders = new Dictionary<string, object?> { [DeadLetterHelper.RetryHeaderName] = "1" };
        DeadLetterHelper.GetRetryCount(strHeaders).Should().Be(1);

        // 6. Byte array
        var bytesHeaders = new Dictionary<string, object?> { [DeadLetterHelper.RetryHeaderName] = Encoding.UTF8.GetBytes("2") };
        DeadLetterHelper.GetRetryCount(bytesHeaders).Should().Be(2);
    }

    [Fact]
    public void EventBusTopology_ExchangesAndQueues_HaveExpectedNaming()
    {
        EventBusTopology.CatalogEventsExchange.Should().Be("catalog-events");
        EventBusTopology.CatalogEventsDlx.Should().Be("catalog-events.dlx");
        EventBusTopology.OrdersEventsExchange.Should().Be("orders-events");

        EventBusTopology.OrdersCatalogSyncQueue.Should().Be("orders.catalog-sync");
        EventBusTopology.OrdersCatalogSyncDlq.Should().Be("orders.catalog-sync.dlq");

        EventBusTopology.ReviewsCatalogSyncQueue.Should().Be("reviews.catalog-sync");
        EventBusTopology.ReviewsCatalogSyncDlq.Should().Be("reviews.catalog-sync.dlq");

        EventBusTopology.ProductPriceChangedRoutingKey.Should().Be("product.price_changed");
        EventBusTopology.ProductRenamedRoutingKey.Should().Be("product.renamed");
        EventBusTopology.ProductCreatedRoutingKey.Should().Be("product.created");
        EventBusTopology.ProductDeletedRoutingKey.Should().Be("product.deleted");
    }

    [Fact]
    public void IntegrationEvent_HasNonEmptyEventId_AndDefaultOccurredAt()
    {
        var @event = new ProductRenamedEvent(10, "Old", "New");
        @event.EventId.Should().NotBeEmpty();
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        @event.ProductId.Should().Be(10);
        @event.OldName.Should().Be("Old");
        @event.NewName.Should().Be("New");
    }

    [Fact]
    public void TracePropagation_InjectAndExtract_PropagatesCorrelationId()
    {
        var props = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                ["x-correlation-id"] = "corr-xyz-987"
            }
        };

        var extracted = TracePropagation.GetCorrelationId(props);
        extracted.Should().Be("corr-xyz-987");
    }
}

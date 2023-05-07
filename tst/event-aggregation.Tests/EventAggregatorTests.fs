namespace SFX.EventAggregation.Tests

open System.Threading
open Xunit
open SFX.EventAggregation

#nowarn "3391"

[<Trait("Category", "Unit")>]
module EventAggregatorTests =

    [<Theory>]
    [<InlineData(10)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``Publish single message to sync subscriber works``(message) =
        let sut : IEventAggregator<int> = createEventAggregator()
        use subscriber = new SingleMessageSyncSubscriber()
        use _ = sut.Subscribe(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        sut.Publish(message)
        subscriber.WaitTillDone()

        Assert.Equal(message, subscriber.ReceivedValue)

    [<Theory>]
    [<InlineData(10)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``Publish single message to async subscriber works``(message) =
        let sut : IEventAggregator<int> = createEventAggregator()
        use subscriber = new SingleMessageAsyncSubscriber()
        use _ = sut.SubscribeAsync(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        sut.Publish(message)
        subscriber.WaitTillDone()

        Assert.Equal(message, subscriber.ReceivedValue)

    [<Theory>]
    [<InlineData(10)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``Publish multiple messages to sync subscriber works``(length) =
        let messages = rndIntArray length
        let sut : IEventAggregator<int> = createEventAggregator()
        use subscriber = new SyncSubscriber(length)
        use _ = sut.Subscribe(subscriber, Unchecked.defaultof<SynchronizationContext>, false)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.WaitTillDone()

        Assert.True(messages |> Array.fold (fun ok message -> ok && subscriber.ReceivedValues |> Array.contains(message)) true)

    [<Theory>]
    [<InlineData(10)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``Publish multiple messages to async subscriber works``(length) =
        let messages = rndIntArray length
        let sut : IEventAggregator<int> = createEventAggregator()
        use subscriber = new AsyncSubscriber(length)
        use _ = sut.SubscribeAsync(subscriber, Unchecked.defaultof<SynchronizationContext>, false)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.WaitTillDone()

        Assert.True(messages |> Array.fold (fun ok message -> ok && subscriber.ReceivedValues |> Array.contains(message)) true)

    [<Theory>]
    [<InlineData(10)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``Publish multiple messages to sync subscriber with serialization works``(length) =
        let messages = rndIntArray length
        let sut : IEventAggregator<int> = createEventAggregator()
        use subscriber = new SyncSubscriber(length)
        use _ = sut.Subscribe(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.WaitTillDone()

        let receivedValues = subscriber.ReceivedValues
        Assert.True((messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true)

    [<Theory>]
    [<InlineData(10)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``Publish multiple messages to async subscriber with serialization works``(length) =
        let messages = rndIntArray length
        let sut : IEventAggregator<int> = createEventAggregator()
        use subscriber = new AsyncSubscriber(length)
        use _ = sut.SubscribeAsync(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.WaitTillDone()

        let receivedValues = subscriber.ReceivedValues 
        Assert.True((messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true)
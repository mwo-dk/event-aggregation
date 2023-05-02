﻿namespace SFX.EventAggregation.Tests

open System
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open System.Threading
open System.Threading.Tasks
open type System.Threading.Interlocked
open Xunit
open FsCheck
open FsCheck.Xunit
open Moq
open SFX.EventAggregation

#nowarn "3391"

[<Trait("Category", "Unit")>]
module EventAggregatorTests =

    [<Property>]
    let ``Publish single message to sync subscriber works``(message) =
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = SingleMessageSyncSubscriber()
        use _ = sut.Subscribe(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        sut.Publish(message)
        subscriber.WaitTillDone()

        message = subscriber.ReceivedValue

    [<Property>]
    let ``Publish single message to async subscriber works``(message) =
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = SingleMessageAsyncSubscriber()
        use _ = sut.SubscribeAsync(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        sut.Publish(message)
        subscriber.WaitTillDone()

        message = subscriber.ReceivedValue

    [<Property>]
    let ``Publish multiple messages to sync subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = SyncSubscriber(messages.Length)
        use _ = sut.Subscribe(subscriber, Unchecked.defaultof<SynchronizationContext>, false)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.WaitTillDone()

        messages |> Array.fold (fun ok message -> ok && subscriber.ReceivedValues |> List.contains(message)) true

    [<Property>]
    let ``Publish multiple messages to async subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = AsyncSubscriber(messages.Length)
        use _ = sut.SubscribeAsync(subscriber, Unchecked.defaultof<SynchronizationContext>, false)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.WaitTillDone()

        messages |> Array.fold (fun ok message -> ok && subscriber.ReceivedValues |> List.contains(message)) true

    [<Property>]
    let ``Publish multiple messages to sync subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = SyncSubscriber(messages.Length)
        use _ = sut.Subscribe(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.WaitTillDone()

        let receivedValues = subscriber.ReceivedValues |> List.toArray
        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    [<Property>]
    let ``Publish multiple messages to async subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = AsyncSubscriber(messages.Length)
        use _ = sut.SubscribeAsync(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.WaitTillDone()

        let receivedValues = subscriber.ReceivedValues |> List.toArray
        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

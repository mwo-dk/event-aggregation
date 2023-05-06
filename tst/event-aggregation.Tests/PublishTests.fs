namespace SFX.EventAggregation.Tests

open System.Threading
open System.Threading.Tasks
open Xunit
open FsCheck
open FsCheck.Xunit
open Moq
open SFX.EventAggregation

#nowarn "3391"

[<Trait("Category", "Unit")>]
module PublishTests =

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``publish single message single message to sync subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = 1L
        let mutable calls = 0L
        let mutable receivedValue = 0
        let subscriber message =
            receivedValue <- message
            if expectedCalls <= inc &calls then
                event.Set() |> ignore

        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        event.WaitOne() |> ignore

        message = receivedValue

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``publish single message to classic async subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = 1L
        let mutable calls = 0L
        let mutable receivedValue = 0
        let subscriber message = 
            receivedValue <- message
            if expectedCalls <= inc &calls then
                event.Set() |> ignore
            Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        event.WaitOne() |> ignore

        message = receivedValue

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``publish single message to task computational expression subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = 1L
        let mutable calls = 0L
        let mutable receivedValue = 0
        let subscriber message = 
            task {
                receivedValue <- message
                if expectedCalls <= inc &calls then
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        event.WaitOne() |> ignore

        message = receivedValue

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(42)>]
    [<InlineData(666)>]
    let ``publish single message to async subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = 1L
        let mutable calls = 0L
        let mutable receivedValue = 0
        let subscriber message = 
            async {
                receivedValue <- message
                if expectedCalls <= inc &calls then
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        event.WaitOne() |> ignore

        message = receivedValue

    [<Property>]
    let ``publish multiple messages single message to sync subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = messages.Length
        let mutable calls = 0L
        let receivedValues : int array = Array.zeroCreate expectedCalls
        let subscriber message =
            let index = (int (inc &calls)) - 1
            receivedValues[index] <- message
            if expectedCalls <= (index+1) then
                event.Set() |> ignore
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && receivedValues |> Array.contains(message)) true

    [<Property>]
    let ``publish multiple messages to classic async subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = messages.Length
        let mutable calls = 0L
        let receivedValues : int array = Array.zeroCreate expectedCalls
        let subscriber message = 
            let index = (int (inc &calls)) - 1
            receivedValues[index] <- message
            if expectedCalls <= (index+1) then
                event.Set() |> ignore
            Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && receivedValues |> Array.contains(message)) true

    [<Property>]
    let ``publish multiple messages to task computational expression subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = messages.Length
        let mutable calls = 0L
        let receivedValues : int array = Array.zeroCreate expectedCalls
        let subscriber message = 
            task {
                let index = (int (inc &calls)) - 1
                receivedValues[index] <- message
                if expectedCalls <= (index+1) then
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && receivedValues |> Array.contains(message)) true

    [<Property>]
    let ``publish multiple messages to async subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = messages.Length
        let mutable calls = 0L
        let receivedValues : int array = Array.zeroCreate expectedCalls
        let subscriber message = 
            async {
                let index = (int (inc &calls)) - 1
                receivedValues[index] <- message
                if expectedCalls <= (index+1) then
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && receivedValues |> Array.contains(message)) true

    [<Property>]
    let ``publish multiple messages single message to sync subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = messages.Length
        let mutable calls = 0L
        let receivedValues : int array = Array.zeroCreate expectedCalls
        let subscriber message =
            let index = (int (inc &calls)) - 1
            receivedValues[index] <- message
            if expectedCalls <= (index+1) then
                event.Set() |> ignore
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        let subscriber = subscriber |> withSerializationOfNotifications true
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    [<Property>]
    let ``publish multiple messages to classic async subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = messages.Length
        let mutable calls = 0L
        let receivedValues : int array = Array.zeroCreate expectedCalls
        let subscriber message = 
            let index = (int (inc &calls)) - 1
            receivedValues[index] <- message
            if expectedCalls <= (index+1) then
                event.Set() |> ignore
            Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        let subscriber = subscriber |> withSerializationOfNotifications true
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    [<Property>]
    let ``publish multiple messages to task computational expression subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = messages.Length
        let mutable calls = 0L
        let receivedValues : int array = Array.zeroCreate expectedCalls
        let subscriber message = 
            task {
                let index = (int (inc &calls)) - 1
                receivedValues[index] <- message
                if expectedCalls <= (index+1) then
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        let subscriber = subscriber |> withSerializationOfNotifications true
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    [<Property>]
    let ``publish multiple messages to async subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        use event = new ManualResetEvent(false)
        let expectedCalls = messages.Length
        let mutable calls = 0L
        let receivedValues : int array = Array.zeroCreate expectedCalls
        let subscriber message = 
            async {
                let index = (int (inc &calls)) - 1
                receivedValues[index] <- message
                if expectedCalls <= (index+1) then
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        let subscriber = subscriber |> withSerializationOfNotifications true
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true
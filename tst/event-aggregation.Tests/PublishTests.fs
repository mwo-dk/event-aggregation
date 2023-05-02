﻿namespace SFX.EventAggregation.Tests

open System.Threading.Tasks
open Xunit
open FsCheck
open FsCheck.Xunit
open Moq
open SFX.EventAggregation

#nowarn "3391"

[<Trait("Category", "Unit")>]
module PublishTests =

    let p = 0

    [<Property>]
    let ``publish single message single message to sync subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        let expectedCalls = 1L
        let mutable calls = 0L
        let mutable receivedValue = 0
        let subscriber message =
            receivedValue <- message
            inc &calls

        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        waitTillDone expectedCalls &calls

        message = receivedValue

    [<Property>]
    let ``publish single message to classic async subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        let expectedCalls = 1L
        let mutable calls = 0L
        let mutable receivedValue = 0
        let subscriber message = 
            receivedValue <- message
            inc &calls
            Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        waitTillDone expectedCalls &calls

        message = receivedValue

    [<Property>]
    let ``publish single message to task computational expression subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        let expectedCalls = 1L
        let mutable calls = 0L
        let mutable receivedValue = 0
        let subscriber message = 
            task {
                receivedValue <- message
                inc &calls
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        waitTillDone expectedCalls &calls

        message = receivedValue

    [<Property>]
    let ``publish single message to async subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        let expectedCalls = 1L
        let mutable calls = 0L
        let mutable receivedValue = 0
        let subscriber message = 
            async {
                receivedValue <- message
                inc &calls
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        waitTillDone expectedCalls &calls

        message = receivedValue

    //[<Property>]
    //let ``publish multiple messages single message to sync subscriber works``(messages: NonEmptyArray<int>) =
    //    let messages = messages.Get
    //    use sut : IEventAggregator<int> = createEventAggregator()
    //    let expectedCalls = messages.Length
    //    let messageContainer = MessageContainer(expectedCalls)
    //    let subscriber message =
    //        messageContainer.Receive(message)
    //    let subscriber : Subscriber<int> = subscriber
    //    let subscriber : Arg<int> = subscriber
    //    use _ = sut |> subscribe subscriber

    //    messages |> Array.iter (fun message -> sut.Publish(message))
    //    messageContainer.WaitTillComplete()

    //    let receivedValues = messageContainer.ReceivedValues
    //    messages |> Array.fold (fun ok message -> ok && receivedValues |> Array.contains(message)) true

    //[<Property>]
    //let ``publish multiple messages to classic async subscriber works``(messages: NonEmptyArray<int>) =
    //    let messages = messages.Get
    //    use sut : IEventAggregator<int> = createEventAggregator()
    //    let expectedCalls = messages.Length
    //    let messageContainer = MessageContainer(expectedCalls)
    //    let subscriber message = 
    //        messageContainer.Receive(message)
    //        Task.CompletedTask
    //    let subscriber : Subscriber<int> = subscriber
    //    let subscriber : Arg<int> = subscriber
    //    use _ = sut |> subscribe subscriber

    //    messages |> Array.iter (fun message -> sut.Publish(message))
    //    messageContainer.WaitTillComplete()

    //    let receivedValues = messageContainer.ReceivedValues
    //    messages |> Array.fold (fun ok message -> ok && receivedValues |> Array.contains(message)) true

    //[<Property>]
    //let ``publish multiple messages to task computational expression subscriber works``(messages: NonEmptyArray<int>) =
    //    let messages = messages.Get
    //    use sut : IEventAggregator<int> = createEventAggregator()
    //    let expectedCalls = messages.Length
    //    let messageContainer = MessageContainer(expectedCalls)
    //    let subscriber message = 
    //        task {
    //            messageContainer.Receive(message)
    //        }
    //    let subscriber : Subscriber<int> = subscriber
    //    let subscriber : Arg<int> = subscriber
    //    use _ = sut |> subscribe subscriber

    //    messages |> Array.iter (fun message -> sut.Publish(message))
    //    messageContainer.WaitTillComplete()

    //    let receivedValues = messageContainer.ReceivedValues
    //    messages |> Array.fold (fun ok message -> ok && receivedValues |> Array.contains(message)) true

    //[<Property>]
    //let ``publish multiple messages to async subscriber works``(messages: NonEmptyArray<int>) =
    //    let messages = messages.Get
    //    use sut : IEventAggregator<int> = createEventAggregator()
    //    let expectedCalls = messages.Length
    //    let messageContainer = MessageContainer(expectedCalls)
    //    let subscriber message = 
    //        async {
    //            messageContainer.Receive(message)
    //        }
    //    let subscriber : Subscriber<int> = subscriber
    //    let subscriber : Arg<int> = subscriber
    //    use _ = sut |> subscribe subscriber

    //    messages |> Array.iter (fun message -> sut.Publish(message))
    //    messageContainer.WaitTillComplete()

    //    let receivedValues = messageContainer.ReceivedValues
    //    messages |> Array.fold (fun ok message -> ok && receivedValues |> Array.contains(message)) true

    //[<Property>]
    //let ``publish multiple messages single message to sync subscriber with serialization works``(messages: NonEmptyArray<int>) =
    //    let messages = messages.Get
    //    use sut : IEventAggregator<int> = createEventAggregator()
    //    let expectedCalls = messages.Length
    //    let messageContainer = MessageContainer(expectedCalls)
    //    let subscriber message =
    //        messageContainer.Receive(message)
    //    let subscriber : Subscriber<int> = subscriber
    //    let subscriber : Arg<int> = subscriber
    //    let subscriber = subscriber |> withSerializationOfNotifications true
    //    use _ = sut |> subscribe subscriber

    //    messages |> Array.iter (fun message -> sut.Publish(message))
    //    messageContainer.WaitTillComplete()

    //    let receivedValues = messageContainer.ReceivedValues
    //    (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    //[<Property>]
    //let ``publish multiple messages to classic async subscriber with serialization works``(messages: NonEmptyArray<int>) =
    //    let messages = messages.Get
    //    use sut : IEventAggregator<int> = createEventAggregator()
    //    let expectedCalls = messages.Length
    //    let messageContainer = MessageContainer(expectedCalls)
    //    let subscriber message = 
    //        messageContainer.Receive(message)
    //        Task.CompletedTask
    //    let subscriber : Subscriber<int> = subscriber
    //    let subscriber : Arg<int> = subscriber
    //    let subscriber = subscriber |> withSerializationOfNotifications true
    //    use _ = sut |> subscribe subscriber

    //    messages |> Array.iter (fun message -> sut.Publish(message))
    //    messageContainer.WaitTillComplete()

    //    let receivedValues = messageContainer.ReceivedValues
    //    (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    //[<Property>]
    //let ``publish multiple messages to task computational expression subscriber with serialization works``(messages: NonEmptyArray<int>) =
    //    let messages = messages.Get
    //    use sut : IEventAggregator<int> = createEventAggregator()
    //    let expectedCalls = messages.Length
    //    let messageContainer = MessageContainer(expectedCalls)
    //    let subscriber message = 
    //        task {
    //            messageContainer.Receive(message)
    //        }
    //    let subscriber : Subscriber<int> = subscriber
    //    let subscriber : Arg<int> = subscriber
    //    let subscriber = subscriber |> withSerializationOfNotifications true
    //    use _ = sut |> subscribe subscriber

    //    messages |> Array.iter (fun message -> sut.Publish(message))
    //    messageContainer.WaitTillComplete()

    //    let receivedValues = messageContainer.ReceivedValues
    //    (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    //[<Property>]
    //let ``publish multiple messages to async subscriber with serialization works``(messages: NonEmptyArray<int>) =
    //    let messages = messages.Get
    //    use sut : IEventAggregator<int> = createEventAggregator()
    //    let expectedCalls = messages.Length
    //    let messageContainer = MessageContainer(expectedCalls)
    //    let subscriber message = 
    //        async {
    //            messageContainer.Receive(message)
    //        }
    //    let subscriber : Subscriber<int> = subscriber
    //    let subscriber : Arg<int> = subscriber
    //    let subscriber = subscriber |> withSerializationOfNotifications true
    //    use _ = sut |> subscribe subscriber

    //    messages |> Array.iter (fun message -> sut.Publish(message))
    //    messageContainer.WaitTillComplete()

    //    let receivedValues = messageContainer.ReceivedValues
    //    (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true
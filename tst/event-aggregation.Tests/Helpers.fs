namespace SFX.EventAggregation.Tests

open System.Threading
open System.Threading.Tasks
open type System.Threading.Interlocked
open Moq
open SFX.EventAggregation

#nowarn "3391"

[<AutoOpen>]
module Helpers = 
    let inline inc (x: int64 byref) = Increment(&x) |> ignore
    let read (x: int64 byref) = Read(&x)
    let waitTillDone expectedCalls (calls: int64 byref) =
        while read &calls < expectedCalls do
            Thread.Sleep(0)

[<Sealed>]
type SingleMessageSyncSubscriber() =
    let expectedCalls = 1L
    let mutable calls = 0L
    let mutable receivedValue = 0

    member _.WaitTillDone() = waitTillDone expectedCalls &calls
    member _.ReceivedValue = receivedValue

    interface IHandle<int> with
        member _.Handle(message) = 
            receivedValue <- message
            inc &calls

[<Sealed>]
type SingleMessageAsyncSubscriber() =
    let expectedCalls = 1L
    let mutable calls = 0L
    let mutable receivedValue = 0

    member _.WaitTillDone() = waitTillDone expectedCalls &calls
    member _.ReceivedValue = receivedValue

    interface IHandleAsync<int> with
        member _.HandleAsync(message) = 
            receivedValue <- message
            inc &calls
            Task.CompletedTask

//type MessageContainer(expectedNumberOfMessages) =
//    let mutable messageCount = 0L
//    let innerContainer = System.Collections.Concurrent.ConcurrentDictionary<int64, int>()

//    member _.Receive(value) =
//        inc &messageCount
//        let success = innerContainer.TryAdd(inc &messageCount, value)
//        if not success then
//            raise (System.InvalidOperationException())

[<Sealed>]
type SyncSubscriber(expectedCalls) =
    let mutable calls = 0L
    let _lock = obj()
    let mutable receivedValues = []

    member _.WaitTillDone() = waitTillDone expectedCalls &calls
    member _.ReceivedValues = receivedValues |> List.rev

    interface IHandle<int> with
        member _.Handle(message) = 
            lock _lock (fun () ->
                receivedValues <- message::receivedValues
                inc &calls
            )

[<Sealed>]
type AsyncSubscriber(expectedCalls) =
    let mutable calls = 0L
    let _lock = obj()
    let mutable receivedValues = []

    member _.WaitTillDone() = waitTillDone expectedCalls &calls
    member _.ReceivedValues = receivedValues |> List.rev

    interface IHandleAsync<int> with
        member _.HandleAsync(message) = 
            lock _lock (fun () ->
                receivedValues <- message::receivedValues
                inc &calls
            )
            Task.CompletedTask

type IAction =
    abstract member Act : unit -> bool


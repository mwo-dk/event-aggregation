namespace SFX.EventAggregation.Tests

open System.Threading
open System.Threading.Tasks
open type System.Threading.Interlocked
open Moq
open SFX.EventAggregation

#nowarn "3391"

[<AutoOpen>]
module Helpers = 
    let inc (x: int64 byref) = Increment(&x) |> ignore
    let read (x: int64 byref) = Read(&x)

[<Sealed>] 
type CountableAwaiter(count) =
    let event = new ManualResetEvent(false)
    let mutable visits = 0L

    new() = CountableAwaiter(1L)
    member _.Visit() = 
        inc &visits
        if visits <= count then
            event.Set() |> ignore
    member _.WaitTillDone() = event.WaitOne() |> ignore
    override x.Finalize() = 
        event.Dispose()

[<Sealed>]
type SingleMessageSyncSubscriber() =
    let awaiter = CountableAwaiter()
    let mutable receivedValue = 0

    member _.WaitTillDone() = awaiter.WaitTillDone()
    member _.ReceivedValue = receivedValue

    interface IHandle<int> with
        member _.Handle(message) = 
            receivedValue <- message
            awaiter.Visit()

[<Sealed>]
type SingleMessageAsyncSubscriber() =
    let awaiter = CountableAwaiter()
    let mutable receivedValue = 0

    member _.WaitTillDone() = awaiter.WaitTillDone()
    member _.ReceivedValue = receivedValue

    interface IHandleAsync<int> with
        member _.HandleAsync(message) = 
            receivedValue <- message
            awaiter.Visit()
            Task.CompletedTask

[<Sealed>]
type SyncSubscriber(expectedCalls) =
    let awaiter = CountableAwaiter(int64 expectedCalls)
    let _lock = ()
    let mutable receivedValues = []

    member _.WaitTillDone() = awaiter.WaitTillDone()
    member _.ReceivedValues = receivedValues |> List.rev

    interface IHandle<int> with
        member _.Handle(message) = 
            lock _lock (fun () ->
                receivedValues <- message::receivedValues
                awaiter.Visit()
            )

[<Sealed>]
type AsyncSubscriber(expectedCalls) =
    let awaiter = CountableAwaiter(int64 expectedCalls)
    let _lock = ()
    let mutable receivedValues = []

    member _.WaitTillDone() = awaiter.WaitTillDone()
    member _.ReceivedValues = receivedValues |> List.rev

    interface IHandleAsync<int> with
        member _.HandleAsync(message) = 
            lock _lock (fun () ->
                receivedValues <- message::receivedValues
                awaiter.Visit()
            )
            Task.CompletedTask

type IAction =
    abstract member Act : unit -> bool


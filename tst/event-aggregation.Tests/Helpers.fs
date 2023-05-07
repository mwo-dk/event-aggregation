namespace SFX.EventAggregation.Tests

open System
open System.Threading
open System.Threading.Tasks
open type System.Threading.Interlocked
open SFX.EventAggregation

#nowarn "3391"

[<AutoOpen>]
module Helpers = 
    let inline inc (x: int64 byref) = Increment(&x) 
    let inline read (x: int64 byref) = Read(&x)
    let private _rnd = System.Random()
    let rnd() = _rnd.Next()
    let rndIntArray length = Array.init length (fun _ -> rnd())

[<Sealed>]
type SingleMessageSyncSubscriber() =
    let event = new ManualResetEvent(false)
    let expectedCalls = 1L
    let mutable calls = 0L
    let mutable receivedValue = 0
    let mutable disposeCount = 0L

    member _.WaitTillDone() = event.WaitOne() |> ignore
    member _.ReceivedValue = receivedValue

    interface IHandle<int> with
        member _.Handle(message) =
            if 0L < (read &disposeCount) then
                raise (ObjectDisposedException(typeof<SingleMessageSyncSubscriber>.FullName))
            receivedValue <- message
            if expectedCalls <= inc &calls then
                event.Set() |> ignore
    interface IDisposable with
        member _.Dispose() = 
            if 1L = (inc &disposeCount) then
                event.Dispose()

[<Sealed>]
type SingleMessageAsyncSubscriber() =
    let event = new ManualResetEvent(false)
    let expectedCalls = 1L
    let mutable calls = 0L
    let mutable receivedValue = 0
    let mutable disposeCount = 0L

    member _.WaitTillDone() = event.WaitOne() |> ignore
    member _.ReceivedValue = receivedValue

    interface IHandleAsync<int> with
        member _.HandleAsync(message) = 
            if 0L < (read &disposeCount) then
                raise (ObjectDisposedException(typeof<SingleMessageAsyncSubscriber>.FullName))
            receivedValue <- message
            if expectedCalls <= inc &calls then
                event.Set() |> ignore
            Task.CompletedTask
    interface IDisposable with
        member _.Dispose() = 
            if 1L = (inc &disposeCount) then
                event.Dispose()

[<Sealed>]
type SyncSubscriber(expectedCalls) =
    let event = new ManualResetEvent(false)
    let mutable calls = 0L
    let receivedValues = Array.zeroCreate expectedCalls
    let mutable disposeCount = 0L

    member _.WaitTillDone() = event.WaitOne() |> ignore
    member _.ReceivedValues = receivedValues

    interface IHandle<int> with
        member _.Handle(message) =
            if 0L < (read &disposeCount) then
                raise (ObjectDisposedException(typeof<SyncSubscriber>.FullName))
            let index = (int (inc &calls)) - 1
            receivedValues[index] <- message
            if expectedCalls <= (index+1) then
                event.Set() |> ignore
    interface IDisposable with
        member _.Dispose() = 
            if 1L = (inc &disposeCount) then
                event.Dispose()

[<Sealed>]
type AsyncSubscriber(expectedCalls) =
    let event = new ManualResetEvent(false)
    let mutable calls = 0L
    let receivedValues = Array.zeroCreate expectedCalls
    let mutable disposeCount = 0L
    member private _.IsDisposed() = 0L < (read &disposeCount)

    member _.WaitTillDone() = event.WaitOne() |> ignore
    member _.ReceivedValues = receivedValues

    interface IHandleAsync<int> with
        member _.HandleAsync(message) = 
            if 0L < (read &disposeCount) then
                raise (ObjectDisposedException(typeof<AsyncSubscriber>.FullName))
            let index = (int (inc &calls)) - 1
            receivedValues[index] <- message
            if expectedCalls <= (index+1) then
                event.Set() |> ignore
            Task.CompletedTask
    interface IDisposable with
        member _.Dispose() = 
            if 1L = (inc &disposeCount) then
                event.Dispose()

type IAction =
    abstract member Act : unit -> bool
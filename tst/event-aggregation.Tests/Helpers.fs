namespace SFX.EventAggregation.Tests

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

[<AutoOpen>]
module Helpers = 
    let inc (x: int64 byref) = Increment(&x) |> ignore
    let read (x: int64 byref) = Read(&x)
    let waitTillDone expected (x: int64 byref) =
        while read &x < expected do
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

[<Sealed>]
type SyncSubscriber(expectedCalls) =
    let mutable calls = 0L
    let mutable receivedValues = []

    member _.WaitTillDone() = waitTillDone expectedCalls &calls
    member _.ReceivedValues = receivedValues |> List.rev

    interface IHandle<int> with
        member _.Handle(message) = 
            receivedValues <- message::receivedValues
            inc &calls

[<Sealed>]
type AsyncSubscriber(expectedCalls) =
    let mutable calls = 0L
    let mutable receivedValues = []

    member _.WaitTillDone() = waitTillDone expectedCalls &calls
    member _.ReceivedValues = receivedValues |> List.rev

    interface IHandleAsync<int> with
        member _.HandleAsync(message) = 
            receivedValues <- message::receivedValues
            inc &calls
            Task.CompletedTask

type IAction =
    abstract member Act : unit -> bool


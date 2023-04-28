namespace SFX.EventAggregation

open System.Threading
open System.Threading.Tasks

[<AutoOpen>]
module Impl =

    type IHandle<'a> =
        abstract member Handle : 'a -> unit

    let toIHandle f =
        {new IHandle<'a> with
            member _.Handle message = f message}

    type IHandleAsync<'a> =
        abstract member HandleAsync : 'a*CancellationToken -> Task

    let toIHandleAsync f =
        {new IHandleAsync<'a> with
            member _.HandleAsync(message, cancellationToken) = Task.FromResult(Async.RunSynchronously(f message, cancellationToken = cancellationToken))}

    type SubscriptionId = int64 

    type IEventAggregator<'a> =
        abstract member Subscribe : IHandle<'a>*SynchronizationContext*bool -> SubscriptionId
        abstract member Subscribe : IHandleAsync<'a>*SynchronizationContext*bool -> SubscriptionId
        abstract member Unsubscribe : SubscriptionId -> bool
        abstract member Publish : 'a -> unit

    type IEventAggregatorFactory =
        abstract member Create<'a> : unit -> IEventAggregator<'a>

    type EventAggregatorName = string
    type IEventAggregatorRepository =
        abstract member GetEventAggregator<'a> : unit -> IEventAggregator<'a>
        abstract member GetEventAggregator<'a> : EventAggregatorName -> IEventAggregator<'a>

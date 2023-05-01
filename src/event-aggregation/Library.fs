namespace SFX.EventAggregation

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open type System.Threading.Interlocked

type IHandle<'a> =
    abstract member Handle : 'a -> unit

type IHandleAsync<'a> =
    abstract member HandleAsync : 'a -> Task

type SubscriptionId = int64

type IEventAggregator<'a> =
    inherit IDisposable
    abstract member Subscribe : IHandle<'a>*SynchronizationContext*bool -> IDisposable
    abstract member SubscribeAsync : IHandleAsync<'a>*SynchronizationContext*bool -> IDisposable
    abstract member Unsubscribe : IDisposable -> unit
    abstract member Publish : 'a -> unit

type Subscription(unsubscribe: unit -> bool) =
    let mutable isDisposed = false

    member _.IsDisposed = isDisposed
    member private _.CleanUp() =
        if not isDisposed then
            unsubscribe() |> ignore
            isDisposed <- true
    override x.Finalize() = x.CleanUp()
    interface IDisposable with
        member x.Dispose() = 
            x.CleanUp()
            GC.SuppressFinalize(x)

type EventAggregator<'a>() =
    let mutable isDisposed = false
    let mutable newSubscription = 0L
    let subscriptions = ConcurrentDictionary<SubscriptionId, Subscription*ActionBlock<'a>>()

    let createActionBlock (subscriber: IHandle<'a>) 
        unsubscribe
        (synchronizationContext: SynchronizationContext) 
        serializeNotification =
        let options = ExecutionDataflowBlockOptions()
        if serializeNotification then
            options.MaxDegreeOfParallelism <- 1
        let reference = WeakReference(subscriber)
            
        ActionBlock<'a>(fun message ->
            let doPublish(_: obj) =
                try
                    if isNull reference.Target then
                        unsubscribe() |> ignore
                    else
                        (reference.Target :?> IHandle<'a>).Handle(message)
                with
                | _ -> ()
            if isNull synchronizationContext then
                doPublish null
            else synchronizationContext.Post(doPublish, null)
        )
    let createAsyncActionBlock (subscriber: IHandleAsync<'a>) 
        unsubscribe
        (synchronizationContext: SynchronizationContext) 
        serializeNotification =
        let options = ExecutionDataflowBlockOptions()
        if serializeNotification then
            options.MaxDegreeOfParallelism <- 1
        let reference = WeakReference(subscriber)
            
        ActionBlock<'a>(fun message ->
            let doPublish(_: obj) =
                try
                    if isNull reference.Target then
                        unsubscribe() |> ignore
                    else
                        (reference.Target :?> IHandleAsync<'a>).HandleAsync(message) |> 
                        Async.AwaitTask |> 
                        Async.RunSynchronously
                with
                | _ -> ()
            if isNull synchronizationContext then
                doPublish null
            else synchronizationContext.Post(doPublish, null)
        )

    interface IEventAggregator<'a> with
        member _.Subscribe(subscriber: IHandle<'a>, synchronizationContext: SynchronizationContext, serializeNotification: bool) = 
            if isDisposed then
                raise (ObjectDisposedException(typeof<IEventAggregator<'a>>.FullName))
            let subscriptionId = Increment(ref newSubscription)
            let unsubscribe() =
                let success, (_, actionBlock) = 
                    subscriptions.TryRemove(subscriptionId)
                if success && actionBlock |> isNull |> not then
                    actionBlock.Complete()
                success
            let subscription = new Subscription(unsubscribe)
            let actionBlock = createActionBlock subscriber unsubscribe synchronizationContext serializeNotification
            let subscribed =
                subscriptions.TryAdd(subscriptionId, (subscription, actionBlock))
            if not subscribed then
                raise (InvalidOperationException("Subscription failed"))
            subscription

        member _.SubscribeAsync(subscriber: IHandleAsync<'a>, synchronizationContext: SynchronizationContext, serializeNotification: bool) = 
            if isDisposed then
                raise (ObjectDisposedException(typeof<IEventAggregator<'a>>.FullName))
            let subscriptionId = Increment(ref newSubscription)
            let unsubscribe() =
                let success, (_, actionBlock) = 
                    subscriptions.TryRemove(subscriptionId)
                if success && actionBlock |> isNull |> not then
                    actionBlock.Complete()
                success
            let subscription = new Subscription(unsubscribe)
            let actionBlock = createAsyncActionBlock subscriber unsubscribe synchronizationContext serializeNotification
            let subscribed =
                subscriptions.TryAdd(subscriptionId, (subscription, actionBlock))
            if not subscribed then
                raise (InvalidOperationException("Subscription failed"))
            subscription

        member _.Unsubscribe(subscription) = 
            if isDisposed then
                raise (ObjectDisposedException(typeof<IEventAggregator<'a>>.FullName))
            if isNull subscription then
                raise (ArgumentNullException(nameof subscription))
            subscription.Dispose()

        member _.Publish(message) =
            if isDisposed then
                raise (ObjectDisposedException(typeof<IEventAggregator<'a>>.FullName))
            let subscribers = subscriptions.Values
            subscribers |> 
                Seq.iter (fun (subscription, handler) -> 
                    if not subscription.IsDisposed then handler.Post(message) |> ignore)

    member private _.CleanUp() = 
        if not isDisposed then
            isDisposed <- true 
            let subscribers = subscriptions.Values
            subscribers |> 
                Seq.iter (fun (subscription, _) -> 
                    if not subscription.IsDisposed then (subscription :> IDisposable).Dispose())
            
    override x.Finalize() = x.CleanUp()
    interface IDisposable with
        member x.Dispose() = 
            x.CleanUp()
            GC.SuppressFinalize(x)

[<AutoOpen>]
module Impl =

    type Handle<'a> =
    | H of IHandle<'a>
    | HA of IHandleAsync<'a>

    type SyncSubscriber<'a> = 'a -> unit
    type TaskSubscriber<'a> = 'a -> Task
    type TaskComputationExpressionSubscriber<'a> = 'a -> Task<unit>
    type AsyncSubscriber<'a> = 'a -> Async<unit>
    type Subscriber<'a> =
    | S of SyncSubscriber<'a>
    | T of TaskSubscriber<'a>
    | TC of TaskComputationExpressionSubscriber<'a>
    | A of AsyncSubscriber<'a>
    with
        static member op_Implicit(x: SyncSubscriber<'a>) = S x
        static member op_Implicit(x: TaskSubscriber<'a>) = T x
        static member op_Implicit(x: TaskComputationExpressionSubscriber<'a>) = TC x
        static member op_Implicit(x: AsyncSubscriber<'a>) = A x

    let syncSubscriber f = S f
    let taskSubscriber f = T f
    let taskComputationExpressionSubscriber f = TC f
    let asyncSubscriber f = A f

    let toHandle x =
        match x with
        | S f ->
            {new IHandle<'a> with
                member _.Handle message = f message} |> H
        | T f ->
            {new IHandleAsync<'a> with
                member _.HandleAsync(message) = f message} |> HA
        | TC f ->
            {new IHandleAsync<'a> with
                member _.HandleAsync(message) = f message} |> HA
        | A f ->
            {new IHandle<'a> with
                member _.Handle message = f message |> Async.RunSynchronously} |> H
    
    type Arg<'a> =
    | AS of Subscriber<'a>
    | ASC of Subscriber<'a>*SynchronizationContext
    | ASS of Subscriber<'a>*bool
    | ASCS of Subscriber<'a>*SynchronizationContext*bool
    with
        static member op_Implicit(x: Subscriber<'a>) = AS x

    let arg x = AS x
    let simplify arg =
        match arg with
        | ASC (x, y) when isNull y -> AS x
        | ASS (x, y) when not y -> AS x
        | ASCS (x, y, z) when isNull y && not z -> AS x
        | ASCS (x, y, z) when isNull y -> ASS (x, z)
        | ASCS (x, y, z) when not z -> ASC (x, y)
        | _ -> arg
    let withSynchronizationContext x arg =
        match arg with 
        | AS f -> ASC (f, x) 
        | ASC (f, _) -> ASC (f, x)
        | ASS (f, y) -> ASCS (f, x, y)
        | ASCS (f, _, y) -> ASCS (f, x, y)
    let withSerializationOfNotifications x arg =
        match arg with
        | AS f -> ASS (f, x)
        | ASC (f, y) -> ASCS (f, y, x)
        | ASS (f, _) -> ASS (f, x)
        | ASCS (f, y, _) -> ASCS (f, y, x)

    let createEventAggregator() = new EventAggregator<'a>() :> IEventAggregator<'a>
    
    let subscribe arg (eventAggregator: IEventAggregator<'a>) =
        match arg |> simplify with
        | AS subscriber -> 
            match subscriber |> toHandle with
            | H x -> eventAggregator.Subscribe(x, null, false)
            | HA x -> eventAggregator.SubscribeAsync(x, null, false)
        | ASC (subscriber, synchronizationContext) -> 
            match subscriber |> toHandle with
            | H x -> eventAggregator.Subscribe(x, synchronizationContext, false)
            | HA x -> eventAggregator.SubscribeAsync(x, synchronizationContext, false)
        | ASS (subscriber, serializeNotification) -> 
            match subscriber |> toHandle with
            | H x -> eventAggregator.Subscribe(x, null, serializeNotification)
            | HA x -> eventAggregator.SubscribeAsync(x, null, serializeNotification)
        | ASCS (subscriber, synchronizationContext, serializeNotification) -> 
            match subscriber |> toHandle with
            | H x -> eventAggregator.Subscribe(x, synchronizationContext, serializeNotification)
            | HA x -> eventAggregator.SubscribeAsync(x, synchronizationContext, serializeNotification)

    let unsubscribe (subscription: IDisposable) = subscription.Dispose()

    let publish message (eventAggregator: IEventAggregator<'a>) =
        eventAggregator.Publish(message)
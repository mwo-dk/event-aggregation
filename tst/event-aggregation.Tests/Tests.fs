namespace SFX.EventAggregation.Tests

open System
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open System.Threading
open System.Threading.Tasks
open Xunit
open FsCheck
open FsCheck.Xunit
open Moq
open SFX.EventAggregation

#nowarn "3391"

[<Sealed>]
type SingleMessageSyncSubscriber() =
    let event = new ManualResetEvent(false)
    let mutable receivedValue = 0

    member _.Event = event
    member _.ReceivedValue = receivedValue

    interface IHandle<int> with
        member _.Handle(message) = 
            receivedValue <- message
            event.Set() |> ignore

[<Sealed>]
type SingleMessageAsyncSubscriber() =
    let event = new ManualResetEvent(false)
    let mutable receivedValue = 0

    member _.Event = event
    member _.ReceivedValue = receivedValue

    interface IHandleAsync<int> with
        member _.HandleAsync(message) = 
            receivedValue <- message
            event.Set() |> ignore
            Task.CompletedTask

[<Sealed>]
type SyncSubscriber(count) =
    let event = new ManualResetEvent(false)
    let mutable receivedValues = []

    member _.Event = event
    member _.ReceivedValues = receivedValues |> List.rev

    interface IHandle<int> with
        member _.Handle(message) = 
            receivedValues <- message::receivedValues
            if count = (receivedValues |> List.length) then
                event.Set() |> ignore

[<Sealed>]
type AsyncSubscriber(count) =
    let event = new ManualResetEvent(false)
    let mutable receivedValues = []

    member _.Event = event
    member _.ReceivedValues = receivedValues |> List.rev

    interface IHandleAsync<int> with
        member _.HandleAsync(message) = 
            receivedValues <- message::receivedValues
            if count = (receivedValues |> List.length) then
                event.Set() |> ignore
            Task.CompletedTask

type IAction =
    abstract member Act : unit -> bool

[<Trait("Category", "Unit")>]
module Tests =

    [<Fact>]
    let ``Subscription is initially not disposed``() =
        let mock = Mock<IAction>()
        let obj = mock.Object
        let act() = obj.Act()
        let sut = new Subscription(act)

        Assert.False(sut.IsDisposed)

    [<Fact>]
    let ``Subscription Dispose unsubscribes``() =
        let mock = Mock<IAction>()
        let obj = mock.Object
        let act() = obj.Act()
        let sut = new Subscription(act)
        (sut :> IDisposable).Dispose()

        Assert.True(sut.IsDisposed)

        let quotation = 
            <@ Func<IAction, bool>(
                fun (x: IAction) -> x.Act()) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IAction, bool>>>
        try
            mock.Verify(quotation, Times.Once)
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Fact>]
    let ``Subscription Dispose unsubscribes once``() =
        let mock = Mock<IAction>()
        let obj = mock.Object
        let act() = obj.Act()
        let sut = new Subscription(act)
        (sut :> IDisposable).Dispose()
        (sut :> IDisposable).Dispose() // A second time

        Assert.True(sut.IsDisposed)

        let quotation = 
            <@ Func<IAction, bool>(
                fun (x: IAction) -> x.Act()) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IAction, bool>>>
        try
            mock.Verify(quotation, Times.Once)
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Property>]
    let ``Publish single message to sync subscriber works``(message) =
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = SingleMessageSyncSubscriber()
        use _ = sut.Subscribe(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        sut.Publish(message)
        subscriber.Event.WaitOne() |> ignore

        message = subscriber.ReceivedValue

    [<Property>]
    let ``Publish single message to async subscriber works``(message) =
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = SingleMessageAsyncSubscriber()
        use _ = sut.SubscribeAsync(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        sut.Publish(message)
        subscriber.Event.WaitOne() |> ignore

        message = subscriber.ReceivedValue

    [<Property>]
    let ``Publish multiple messages to sync subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = SyncSubscriber(messages.Length)
        use _ = sut.Subscribe(subscriber, Unchecked.defaultof<SynchronizationContext>, false)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.Event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && subscriber.ReceivedValues |> List.contains(message)) true

    [<Property>]
    let ``Publish multiple messages to async subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = AsyncSubscriber(messages.Length)
        use _ = sut.SubscribeAsync(subscriber, Unchecked.defaultof<SynchronizationContext>, false)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.Event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && subscriber.ReceivedValues |> List.contains(message)) true

    [<Property>]
    let ``Publish multiple messages to sync subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = SyncSubscriber(messages.Length)
        use _ = sut.Subscribe(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.Event.WaitOne() |> ignore

        let receivedValues = subscriber.ReceivedValues |> List.toArray
        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    [<Property>]
    let ``Publish multiple messages to async subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        let sut : IEventAggregator<int> = createEventAggregator()
        let subscriber = AsyncSubscriber(messages.Length)
        use _ = sut.SubscribeAsync(subscriber, Unchecked.defaultof<SynchronizationContext>, true)

        messages |> Array.iter (fun message -> sut.Publish(message))
        subscriber.Event.WaitOne() |> ignore

        let receivedValues = subscriber.ReceivedValues |> List.toArray
        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    [<Fact>]
    let ``implicit conversion of sync subscriber works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        match subscriber with
        | S _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``implicit conversion of classic async subscriber works``() =
        let subscriber (_: int) = Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        match subscriber with
        | T _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``implicit conversion of task computational expression subscriber works``() =
        let subscriber (_: int) = task {()}
        let subscriber : Subscriber<int> = subscriber
        match subscriber with
        | TC _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``implicit conversion of async subscriber works``() =
        let subscriber (_: int) = async {()}
        let subscriber : Subscriber<int> = subscriber
        match subscriber with
        | A _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``syncSubscriber works``() =
        let subscriber (_: int) = ()
        let subscriber = syncSubscriber subscriber
        match subscriber with
        | S _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``taskSubscriber works``() =
        let subscriber (_: int) = Task.CompletedTask
        let subscriber = taskSubscriber subscriber
        match subscriber with
        | T _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``taskComputationExpressionSubscriber works``() =
        let subscriber (_: int) = task {()}
        let subscriber = taskComputationExpressionSubscriber subscriber
        match subscriber with
        | TC _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``asyncSubscriber works``() =
        let subscriber (_: int) = async {()}
        let subscriber = asyncSubscriber subscriber
        match subscriber with
        | A _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``toHandle for sync subscriber works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = toHandle subscriber
        match result with
        | H _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``toHandle for classic async subscriber works``() =
        let subscriber (_: int) = Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let result = toHandle subscriber
        match result with
        | HA _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``toHandle for task computational expression subscriber works``() =
        let subscriber (_: int) = task {()}
        let subscriber : Subscriber<int> = subscriber
        let result = toHandle subscriber
        match result with
        | HA _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``toHandle for async subscriber works``() =
        let subscriber (_: int) = async {()}
        let subscriber : Subscriber<int> = subscriber
        let result = toHandle subscriber
        match result with
        | H _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``arg works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber
        match result with
        | AS _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``simplify of AS works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber
        match result |> simplify with
        | AS _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``simplify of ASC with null synchronization context works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSynchronizationContext Unchecked.defaultof<SynchronizationContext>
        match result |> simplify with
        | AS _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``simplify of ASS with no serialization context works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications false
        match result |> simplify with
        | AS _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``simplify of ASCS with null synchronization context and no serialization context works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSynchronizationContext Unchecked.defaultof<SynchronizationContext> |> withSerializationOfNotifications false
        match result |> simplify with
        | AS _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``simplify of ASCS with null synchronization context and serialization context works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSynchronizationContext Unchecked.defaultof<SynchronizationContext> |> withSerializationOfNotifications true
        match result |> simplify with
        | ASS (_, true) -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``simplify of ASCS with no serialization context works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSynchronizationContext SynchronizationContext.Current |> withSerializationOfNotifications false
        match result |> simplify with
        | ASC _ -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``withSynchronizationContext for AS works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber 
        let sc = SynchronizationContext()
        match result |> withSynchronizationContext sc with
        | ASC (_, x) when x = sc -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Fact>]
    let ``withSynchronizationContext for ASC works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSynchronizationContext Unchecked.defaultof<SynchronizationContext>
        let sc = SynchronizationContext()
        match result |> withSynchronizationContext sc with
        | ASC (_, x) when x = sc -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Property>]
    let ``withSynchronizationContext for ASS works``(serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications serializeNotification
        let sc = SynchronizationContext()
        match result |> withSynchronizationContext sc with
        | ASCS (_, x, y) when x = sc && y = serializeNotification -> true
        | _ -> false

    [<Property>]
    let ``withSynchronizationContext for ASCS works``(serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSynchronizationContext Unchecked.defaultof<SynchronizationContext> |> withSerializationOfNotifications serializeNotification
        let sc = SynchronizationContext()
        match result |> withSynchronizationContext sc with
        | ASCS (_, x, y) when x = sc && y = serializeNotification -> true
        | _ -> false

    [<Property>]
    let ``withSerializationOfNotifications for AS works``(oldSerializeNotification, serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications oldSerializeNotification
        match result |> withSerializationOfNotifications serializeNotification with
        | ASS (_, x) when x = serializeNotification -> true
        | _ -> false

    [<Property>]
    let ``withSerializationOfNotifications for ASC works``(serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let sc = SynchronizationContext()
        let result = arg subscriber |> withSynchronizationContext sc
        match result |> withSerializationOfNotifications serializeNotification with
        | ASCS (_, x, y) when x = sc && y = serializeNotification -> true
        | _ -> false

    [<Property>]
    let ``withSerializationOfNotifications for ASS works``(oldSerializeNotification, serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications oldSerializeNotification
        match result |> withSerializationOfNotifications serializeNotification with
        | ASS (_, x) when x = serializeNotification -> true
        | _ -> false

    [<Property>]
    let ``withSerializationOfNotifications for ASCS works``(oldSerializeNotification, serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let sc = SynchronizationContext()
        let result = arg subscriber |> withSynchronizationContext sc |> withSerializationOfNotifications oldSerializeNotification
        match result |> withSerializationOfNotifications serializeNotification with
        | ASCS (_, x, y) when x = sc && y = serializeNotification -> true
        | _ -> false

    [<Property>]
    let ``subscribe with sync subscriber and null synchronization context works``(serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications serializeNotification
        let mock = Mock<IEventAggregator<int>>()
        
        let sut = mock.Object
        subscribe result sut |> ignore
        
        let quotation = 
            <@ Func<IEventAggregator<int>, IDisposable>(
                fun (x: IEventAggregator<int>) -> x.Subscribe(It.IsAny<IHandle<int>>(), null, serializeNotification)) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IEventAggregator<int>, IDisposable>>>
        try
            mock.Verify(quotation, Times.Once)
            true
        with
        | _ -> false

    [<Property>]
    let ``subscribe with sync subscriber and synchronization context works``(serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let sc = SynchronizationContext()
        let result = arg subscriber |> withSynchronizationContext sc |> withSerializationOfNotifications serializeNotification
        let mock = Mock<IEventAggregator<int>>()
        
        let sut = mock.Object
        subscribe result sut |> ignore
        
        let quotation = 
            <@ Func<IEventAggregator<int>, IDisposable>(
                fun (x: IEventAggregator<int>) -> x.Subscribe(It.IsAny<IHandle<int>>(), sc, serializeNotification)) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IEventAggregator<int>, IDisposable>>>
        try
            mock.Verify(quotation, Times.Once)
            true
        with
        | _ -> false

    [<Property>]
    let ``subscribe with classical async subscriber and null synchronization context works``(serializeNotification) =
        let subscriber (_: int) = Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications serializeNotification
        let mock = Mock<IEventAggregator<int>>()
        
        let sut = mock.Object
        subscribe result sut |> ignore
        
        let quotation = 
            <@ Func<IEventAggregator<int>, IDisposable>(
                fun (x: IEventAggregator<int>) -> x.SubscribeAsync(It.IsAny<IHandleAsync<int>>(), null, serializeNotification)) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IEventAggregator<int>, IDisposable>>>
        try
            mock.Verify(quotation, Times.Once)
            true
        with
        | _ -> false

    [<Property>]
    let ``subscribe with classical async subscriber and synchronization context works``(serializeNotification) =
        let subscriber (_: int) = Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let sc = SynchronizationContext()
        let result = arg subscriber |> withSynchronizationContext sc |> withSerializationOfNotifications serializeNotification
        let mock = Mock<IEventAggregator<int>>()
        
        let sut = mock.Object
        subscribe result sut |> ignore
        
        let quotation = 
            <@ Func<IEventAggregator<int>, IDisposable>(
                fun (x: IEventAggregator<int>) -> x.SubscribeAsync(It.IsAny<IHandleAsync<int>>(), sc, serializeNotification)) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IEventAggregator<int>, IDisposable>>>
        try
            mock.Verify(quotation, Times.Once)
            true
        with
        | _ -> false

    [<Property>]
    let ``subscribe with task computational expression subscriber and null synchronization context works``(serializeNotification) =
        let subscriber (_: int) = task {()}
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications serializeNotification
        let mock = Mock<IEventAggregator<int>>()
        
        let sut = mock.Object
        subscribe result sut |> ignore
        
        let quotation = 
            <@ Func<IEventAggregator<int>, IDisposable>(
                fun (x: IEventAggregator<int>) -> x.SubscribeAsync(It.IsAny<IHandleAsync<int>>(), null, serializeNotification)) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IEventAggregator<int>, IDisposable>>>
        try
            mock.Verify(quotation, Times.Once)
            true
        with
        | _ -> false

    [<Property>]
    let ``subscribe with task computational expression subscriber and synchronization context works``(serializeNotification) =
        let subscriber (_: int) = task {()}
        let subscriber : Subscriber<int> = subscriber
        let sc = SynchronizationContext()
        let result = arg subscriber |> withSynchronizationContext sc |> withSerializationOfNotifications serializeNotification
        let mock = Mock<IEventAggregator<int>>()
        
        let sut = mock.Object
        subscribe result sut |> ignore
        
        let quotation = 
            <@ Func<IEventAggregator<int>, IDisposable>(
                fun (x: IEventAggregator<int>) -> x.SubscribeAsync(It.IsAny<IHandleAsync<int>>(), sc, serializeNotification)) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IEventAggregator<int>, IDisposable>>>
        try
            mock.Verify(quotation, Times.Once)
            true
        with
        | _ -> false

    [<Property>]
    let ``subscribe with async subscriber and null synchronization context works``(serializeNotification) =
        let subscriber (_: int) = async {()}
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications serializeNotification
        let mock = Mock<IEventAggregator<int>>()
        
        let sut = mock.Object
        subscribe result sut |> ignore
        
        let quotation = 
            <@ Func<IEventAggregator<int>, IDisposable>(
                fun (x: IEventAggregator<int>) -> x.Subscribe(It.IsAny<IHandle<int>>(), null, serializeNotification)) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IEventAggregator<int>, IDisposable>>>
        try
            mock.Verify(quotation, Times.Once)
            true
        with
        | _ -> false

    [<Property>]
    let ``subscribe with async subscriber and synchronization context works``(serializeNotification) =
        let subscriber (_: int) = async {()}
        let subscriber : Subscriber<int> = subscriber
        let sc = SynchronizationContext()
        let result = arg subscriber |> withSynchronizationContext sc |> withSerializationOfNotifications serializeNotification
        let mock = Mock<IEventAggregator<int>>()
        
        let sut = mock.Object
        subscribe result sut |> ignore
        
        let quotation = 
            <@ Func<IEventAggregator<int>, IDisposable>(
                fun (x: IEventAggregator<int>) -> x.Subscribe(It.IsAny<IHandle<int>>(), sc, serializeNotification)) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Func<IEventAggregator<int>, IDisposable>>>
        try
            mock.Verify(quotation, Times.Once)
            true
        with
        | _ -> false

    [<Fact>]
    let ``unsubscribe simply disposes``() =
        let mock = Mock<IDisposable>()

        let sut = mock.Object
        unsubscribe sut

        let quotation =
            <@ Action<IDisposable>(
                fun (x: IDisposable) -> x.Dispose()) @> |>
            LeafExpressionConverter.QuotationToExpression |>
            unbox<Expression<Action<IDisposable>>>
        try 
            mock.Verify(quotation, Times.Once)
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Property>]
    let ``publish single message single message to sync subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValue = 0
        use event = new ManualResetEvent(false)
        let subscriber message =
            receivedValue <- message
            event.Set() |> ignore
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        event.WaitOne() |> ignore

        message = receivedValue

    [<Property>]
    let ``publish single message to classic async subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValue = 0
        use event = new ManualResetEvent(false)
        let subscriber message = 
            receivedValue <- message
            event.Set() |> ignore
            Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        event.WaitOne() |> ignore

        message = receivedValue

    [<Property>]
    let ``publish single message to task computational expression subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValue = 0
        use event = new ManualResetEvent(false)
        let subscriber message = 
            task {
                receivedValue <- message
                event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        sut |> publish message
        event.WaitOne() |> ignore

        message = receivedValue

    [<Property>]
    let ``publish single message to async subscriber works``(message) =
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValue = 0
        use event = new ManualResetEvent(false)
        let subscriber message = 
            async {
                receivedValue <- message
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
        let mutable receivedValues : int list= []
        use event = new ManualResetEvent(false)
        let subscriber message =
            receivedValues <- message::receivedValues
            if (messages.Length) = (receivedValues |> List.length) then 
                event.Set() |> ignore
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && receivedValues |> List.contains(message)) true

    [<Property>]
    let ``publish multiple messages to classic async subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValues : int list= []
        use event = new ManualResetEvent(false)
        let subscriber message = 
            receivedValues <- message::receivedValues
            if (messages.Length) = (receivedValues |> List.length) then 
                event.Set() |> ignore
            Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && receivedValues |> List.contains(message)) true

    [<Property>]
    let ``publish multiple messages to task computational expression subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValues : int list= []
        use event = new ManualResetEvent(false)
        let subscriber message = 
            task {
                receivedValues <- message::receivedValues
                if (messages.Length) = (receivedValues |> List.length) then 
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && receivedValues |> List.contains(message)) true

    [<Property>]
    let ``publish multiple messages to async subscriber works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValues : int list= []
        use event = new ManualResetEvent(false)
        let subscriber message = 
            async {
                receivedValues <- message::receivedValues
                if (messages.Length) = (receivedValues |> List.length) then 
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        messages |> Array.fold (fun ok message -> ok && receivedValues |> List.contains(message)) true


    [<Property>]
    let ``publish multiple messages single message to sync subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValues : int list= []
        use event = new ManualResetEvent(false)
        let subscriber message =
            receivedValues <- message::receivedValues
            if (messages.Length) = (receivedValues |> List.length) then 
                event.Set() |> ignore
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        let subscriber = subscriber |> withSerializationOfNotifications true
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        let receivedValues = receivedValues |> List.rev |> List.toArray
        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    [<Property>]
    let ``publish multiple messages to classic async subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValues : int list= []
        use event = new ManualResetEvent(false)
        let subscriber message = 
            receivedValues <- message::receivedValues
            if (messages.Length) = (receivedValues |> List.length) then 
                event.Set() |> ignore
            Task.CompletedTask
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        let subscriber = subscriber |> withSerializationOfNotifications true
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        let receivedValues = receivedValues |> List.rev |> List.toArray
        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    [<Property>]
    let ``publish multiple messages to task computational expression subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValues : int list= []
        use event = new ManualResetEvent(false)
        let subscriber message = 
            task {
                receivedValues <- message::receivedValues
                if (messages.Length) = (receivedValues |> List.length) then 
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        let subscriber = subscriber |> withSerializationOfNotifications true
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        let receivedValues = receivedValues |> List.rev |> List.toArray
        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true

    [<Property>]
    let ``publish multiple messages to async subscriber with serialization works``(messages: NonEmptyArray<int>) =
        let messages = messages.Get
        use sut : IEventAggregator<int> = createEventAggregator()
        let mutable receivedValues : int list= []
        use event = new ManualResetEvent(false)
        let subscriber message = 
            async {
                receivedValues <- message::receivedValues
                if (messages.Length) = (receivedValues |> List.length) then 
                    event.Set() |> ignore
            }
        let subscriber : Subscriber<int> = subscriber
        let subscriber : Arg<int> = subscriber
        let subscriber = subscriber |> withSerializationOfNotifications true
        use _ = sut |> subscribe subscriber

        messages |> Array.iter (fun message -> sut.Publish(message))
        event.WaitOne() |> ignore

        let receivedValues = receivedValues |> List.rev |> List.toArray
        (messages, receivedValues) ||> Array.fold2 (fun ok x y -> ok && x = y) true
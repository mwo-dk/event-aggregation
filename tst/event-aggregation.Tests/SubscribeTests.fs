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

[<Trait("Category", "Unit")>]
module SubscribeTests =

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
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
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
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
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
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
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
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
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
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
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
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
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
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
            Assert.True(true)
        with
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
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
            Assert.True(true)
        with
        | _ -> Assert.True(false)

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


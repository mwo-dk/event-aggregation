namespace SFX.EventAggregation.Tests

open System
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open Xunit
open Moq
open SFX.EventAggregation

#nowarn "3391"

[<Trait("Category", "Unit")>]
module SubscriptionTests =

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
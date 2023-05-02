namespace SFX.EventAggregation.Tests

open System.Threading.Tasks
open Xunit
open SFX.EventAggregation

#nowarn "3391"

[<Trait("Category", "Unit")>]
module ConversionTests = 

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
namespace SFX.EventAggregation.Tests

open System.Threading.Tasks
open Xunit
open SFX.EventAggregation

#nowarn "3391"

[<Trait("Category", "Unit")>]
module ToHandleTests =

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
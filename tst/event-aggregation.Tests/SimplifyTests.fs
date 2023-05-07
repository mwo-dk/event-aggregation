namespace SFX.EventAggregation.Tests

open System.Threading
open Xunit
open SFX.EventAggregation

#nowarn "3391"

[<Trait("Category", "Unit")>]
module SimplifyTests =

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


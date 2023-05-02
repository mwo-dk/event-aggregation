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
module ArgTests =

    [<Fact>]
    let ``arg works``() =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber
        match result with
        | AS _ -> Assert.True(true)
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

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
    let ``withSynchronizationContext for ASCS works``(serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSynchronizationContext Unchecked.defaultof<SynchronizationContext> |> withSerializationOfNotifications serializeNotification
        let sc = SynchronizationContext()
        match result |> withSynchronizationContext sc with
        | ASCS (_, x, y) when x = sc && y = serializeNotification -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``withSerializationOfNotifications for AS works``(oldSerializeNotification, serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications oldSerializeNotification
        match result |> withSerializationOfNotifications serializeNotification with
        | ASS (_, x) when x = serializeNotification -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
    let ``withSerializationOfNotifications for ASC works``(serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let sc = SynchronizationContext()
        let result = arg subscriber |> withSynchronizationContext sc
        match result |> withSerializationOfNotifications serializeNotification with
        | ASCS (_, x, y) when x = sc && y = serializeNotification -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``withSerializationOfNotifications for ASS works``(oldSerializeNotification, serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let result = arg subscriber |> withSerializationOfNotifications oldSerializeNotification
        match result |> withSerializationOfNotifications serializeNotification with
        | ASS (_, x) when x = serializeNotification -> Assert.True(true)
        | _ -> Assert.True(false)

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``withSerializationOfNotifications for ASCS works``(oldSerializeNotification, serializeNotification) =
        let subscriber (_: int) = ()
        let subscriber : Subscriber<int> = subscriber
        let sc = SynchronizationContext()
        let result = arg subscriber |> withSynchronizationContext sc |> withSerializationOfNotifications oldSerializeNotification
        match result |> withSerializationOfNotifications serializeNotification with
        | ASCS (_, x, y) when x = sc && y = serializeNotification -> Assert.True(true)
        | _ -> Assert.True(false)


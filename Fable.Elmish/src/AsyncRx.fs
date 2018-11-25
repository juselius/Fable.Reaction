namespace Reaction

open Fable.Core
open Fable.Import.Browser
open System

/// AsyncRx Extensions
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncRx =
    /// Returns an observable that produces a notification when the
    /// promise resolves. The observable will also complete after
    /// producing an event.
    let ofPromise (pr: Fable.Import.JS.Promise<_>) =
        Create.ofAsyncWorker(fun obv _ -> async {
            try
                let! result = Async.AwaitPromise pr
                do! obv.OnNextAsync result
                do! obv.OnCompletedAsync ()
            with
            | ex ->
                do! obv.OnErrorAsync ex
        })

    /// Returns an async observable of mouse events.
    let ofMouseMove () : IAsyncObservable<Fable.Import.Browser.MouseEvent> =
        let subscribe (obv: IAsyncObserver<Fable.Import.Browser.MouseEvent>) : Async<IAsyncDisposable> =
            async {
                let onMouseMove (ev: Fable.Import.Browser.MouseEvent) =
                    async {
                        do! obv.OnNextAsync ev
                    } |> Async.StartImmediate

                window.addEventListener_mousemove onMouseMove
                let cancel () = async {
                    window.removeEventListener ("mousemove", unbox onMouseMove)
                }
                return AsyncDisposable.Create cancel
            }

        AsyncRx.create subscribe

    /// Websocket channel operator. Passes string items as ws messages to
    /// the server. Received ws messages will be forwarded down stream.
    /// JSON encode/decode of application messages is left to the client.
    let inline channel (uri: string) (source: IAsyncObservable<string>) : IAsyncObservable<string> =
        Reaction.WebSocket.channel uri source

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {'msg}.
    let inline msgChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> 'msg option) (source: IAsyncObservable<'msg>) : IAsyncObservable<'msg> =
        Reaction.WebSocket.msgChannel uri encode decode source

    /// Websocket message channel operator. Items {'msg} will be encoded
    /// to JSON using `encode` and passed as over the ws channel to the server.
    /// Data received on the ws channel as strings (JSON) will be
    /// decoded using `decode` and forwarded down stream as messages {Result<'msg, exn>}.
    let msgResultChannel<'msg> (uri: string) (encode: 'msg -> string) (decode: string -> Result<'msg, exn>) (source: IAsyncObservable<'msg>) : IAsyncObservable<Result<'msg, exn>> =
        Reaction.WebSocket.msgResultChannel uri encode decode source

    /// Turn the observable into a named stream
    let inline toStream (name: 'name) (source: IAsyncObservable<'a>) : Stream<'a, 'name> =
        Stream (source, name)

    [<Obsolete("Do not use. Use `toStream` instead.")>]
    let inline asStream (name: 'name) (source: IAsyncObservable<'a>) : Stream<'a, 'name> =
        Stream (source, name)

    /// Convert stream back to async observable by merging all the streams.
    let inline ofStream (name: 'name) (source: Stream<'a, 'name>) : IAsyncObservable<'a> =
        { new IAsyncObservable<'a> with
            member this.SubscribeAsync obv =
                let rec flatten streams =
                    [
                        for stream in streams do
                            match stream with
                            | Stream (xs, _) ->
                                yield xs
                            | Streams xss ->
                                yield! flatten xss
                            | _ -> ()
                    ]
                async {
                    match source with
                    | Stream (xs, _) ->
                        return! xs.SubscribeAsync obv
                    | Streams xss ->
                        let xs = flatten xss |> AsyncRx.mergeSeq
                        return! xs.SubscribeAsync obv
                    | _ ->
                        return AsyncDisposable.Empty
                }
        }

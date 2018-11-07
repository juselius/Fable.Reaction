# Reactive MVU Architecture

Elmish.Reaction is very similar to [Elm](http://elm-lang.org/) and [Elmish](https://elmish.github.io/) in regards to the [MVU architecture](https://guide.elm-lang.org/architecture/). But when using Elmish.Reaction, we do not need any commands (`Cmd`) or subscriptions with Elmish. Instead we use a [ReactiveX](http://reactivex.io/) (AsyncRx) style query that transforms the stream of messages (`Msg`).

<img src="https://raw.githubusercontent.com/dbrattli/Fable.Reaction/master/gh-pages/images/R-MVU.png" width="550">

* **Model**, application state as immutable data
* **View**, a pure function that takes the model to produce the output view (HTML elements)
* **Message**, a data event that represents a change. Messages are generated by the view, or they may be generated by the reaction query, e.g. timer or initial message events.
* **Update**, a pure function that produces a new model based on a received message and the previous model

In addition, Fable Reaction may also have a reaction query that transforms the "stream" of messages.

* **Query**, a function that takes the message stream and produces a new (transformed) message stream. Note that this also replaces the need for Elm(ish) commands (Cmd) since the reaction is free to produce any messages (out of thin air), transform, filter, time-shift messages or combine side-effects such as web requests (fetch) etc.
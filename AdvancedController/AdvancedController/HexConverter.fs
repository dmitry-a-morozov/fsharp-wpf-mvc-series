namespace Mvc.Wpf.Sample

open System.Windows.Data
open Mvc.Wpf

[<AbstractClass>]
type HexConverterModel() = 
    inherit Model()

    abstract Value : int with get, set

type HexConverterEvents = 
    | OK
    | Cancel

type HexConverterView() =
    inherit View<HexConverterEvents, HexConverterModel, HexConverterWindow>()

    override this.EventStreams = 
        [
            this.Window.OK, OK
            this.Window.Cancel, Cancel
        ]
        |> List.map(fun(button, value) -> button.Click |> Observable.mapTo value)

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Window.Value.Text <- string model.Value
            @>

type HexConverterController(view : IView<_, _>) = 
    inherit SyncController<HexConverterEvents, HexConverterModel>(view)

    override this.InitModel _ = ()
    override this.Dispatcher = function
        | OK -> this.OK
        | Cancel -> 
            fun _ -> view.Close false

    member this.OK(model : HexConverterModel) = 
        ()


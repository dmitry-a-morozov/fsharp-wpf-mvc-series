namespace Mvc.Wpf.Sample

open System
open System.Globalization
open System.Windows.Data
open Mvc.Wpf

[<AbstractClass>]
type HexConverterModel(value) as this = 
    inherit Model()

    do
        this.HexValue <- sprintf "%X" value

    new() = HexConverterModel(0)

    abstract HexValue : string with get, set

    member this.Value =
        match Int32.TryParse(this.HexValue, NumberStyles.HexNumber, null) with 
        | true, value -> Some(value)
        | false, _ -> None

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
                this.Window.Value.Text <- model.HexValue
            @>

type HexConverterController(view : IView<_, _>) = 
    inherit SyncController<HexConverterEvents, HexConverterModel>(view)

    override this.InitModel _ = ()
    override this.Dispatcher = function
        | OK -> this.OK
        | Cancel -> 
            fun _ -> view.Close false

    member this.OK(model : HexConverterModel) = 
        match model.Value with
        | None -> model |> Validation.setError <@ fun m -> m.HexValue @> (sprintf "Cannot parse hex value %s" model.HexValue)
        | Some _ -> view.Close true


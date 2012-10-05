namespace Mvc.Wpf.Sample

open System
open System.Globalization
open System.Windows.Data
open Mvc.Wpf

[<AbstractClass>]
type HexConverterModel() = 
    inherit Model()

    abstract HexValue : string with get, set

    member this.Value 
        with get() = 
            match Int32.TryParse(this.HexValue, NumberStyles.HexNumber, null) with 
            | true, value -> Some(value)
            | false, _ -> None
        and set value = 
            match value with 
            | Some x -> this.HexValue <- sprintf "%X" x
            | None -> invalidArg "value" "Value is None."

type HexConverterEvents = 
    | OK

type HexConverterView() as this =
    inherit View<HexConverterEvents, HexConverterModel, HexConverterWindow>()

    do
        this.CancelButton <- this.Window.Cancel

    override this.EventStreams = 
        [
            this.Window.OK, OK
        ]
        |> List.map(fun(button, value) -> button.Click |> Observable.mapTo value)

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Window.Value.Text <- model.HexValue
            @>

type HexConverterController(view) = 
    inherit SyncController<HexConverterEvents, HexConverterModel>(view)

    override this.InitModel _ = ()
    override this.Dispatcher = function
        | OK -> this.OK

    member this.OK(model : HexConverterModel) = 
        match model.Value with
        | None -> model |> Validation.setError <@ fun m -> m.HexValue @> (sprintf "Cannot parse hex value %s" model.HexValue)
        | Some _ -> view.OK()


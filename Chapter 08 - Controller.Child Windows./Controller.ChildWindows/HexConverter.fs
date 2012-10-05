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
        with get() = Int32.Parse(this.HexValue, NumberStyles.HexNumber)
        and set value = this.HexValue <- sprintf "%X" value

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
        try 
            let _ = model.Value
            view.OK()
        with :? FormatException as why ->  
            let errorMessage = sprintf "Cannot parse hex value %s because %s" model.HexValue why.Message
            model |> Validation.setError <@ fun m -> m.HexValue @> errorMessage


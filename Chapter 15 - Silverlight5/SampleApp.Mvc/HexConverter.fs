namespace SampleApp

open System
open System.Globalization
open System.Windows.Data
open FSharp.Windows

module HexConverter =  

    type Events = OK of (unit -> unit)

    [<AbstractClass>]
    type Model() = 
        inherit FSharp.Windows.Model()

        abstract HexValue : string with get, set
        member this.Value 
            with get() = Int32.Parse(this.HexValue, NumberStyles.HexNumber)
            and set value = this.HexValue <- sprintf "%X" value

    let view() = 
        let result = {
            new Dialog<Events, Model, HexConverterWindow>() with 
                member this.EventStreams = 
                    [
                        this.Control.OK.Click |> Observable.mapTo(OK(this.OK))
                    ]

                member this.SetBindings model = 
                    Binding.TwoWay 
                        <@ 
                            this.Control.Value.Text <- model.HexValue
                        @>
        }
        result.CancelButton <- result.Control.Cancel
        result

    let controller() = 
        Controller.Create(fun(OK(close)) (model : Model) ->
            let isValid, _ = Int32.TryParse(model.HexValue, NumberStyles.HexNumber, null)
            if isValid 
            then close()
            else model |> Validation.setError <@ fun m -> m.HexValue @> (sprintf "Cannot parse hex value %s" model.HexValue)
        )

namespace FSharp.Windows.Sample

open System
open System.Globalization
open System.Windows.Data
open FSharp.Windows

module HexConverter =  

    type Events = ValueChanging of string * (unit -> unit)

    [<AbstractClass>]
    type Model() = 
        inherit FSharp.Windows.Model()

        abstract OkEnabled : bool with get, set
        abstract HexValue : string with get, set
        member this.Value 
            with get() = Int32.Parse(this.HexValue, NumberStyles.HexNumber)
            and set value = this.HexValue <- sprintf "%X" value

    let view() = 
        let result = {
            new View<Events, Model, HexConverterWindow>() with 
                member this.EventStreams = 
                    [
                        this.Window.Value.PreviewTextInput |> Observable.map(fun args -> ValueChanging(args.Text, fun() -> args.Handled <- true))
                    ]

                member this.SetBindings model = 
                    Binding.FromExpression 
                        <@ 
                            this.Window.Value.Text <- model.HexValue
                            this.Window.OK.IsEnabled <- model.OkEnabled
                        @>
        }
        result.CancelButton <- result.Window.Cancel
        result.DefaultOKButton <- result.Window.OK
        result

    let controller() = {
        new SyncController<Events, Model>() with
            member this.InitModel model = 
                model.OkEnabled <- true
            member this.Dispatcher = fun(ValueChanging(text, cancel)) (model : Model) ->
                if Int32.TryParse(text, NumberStyles.HexNumber, null, ref 0)
                then model.OkEnabled <- true
                else cancel()
    }
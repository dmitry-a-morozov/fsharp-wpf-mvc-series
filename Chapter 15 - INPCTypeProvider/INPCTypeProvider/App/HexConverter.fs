namespace FSharp.Windows.Sample

open System
open System.Globalization
open System.Windows.Data
open FSharp.Windows
open FSharp.Windows.UIElements

module HexConverter =  

    type Events = 
        | ValidateHexFormat of string * (unit -> unit)
        | UpdateValue of string

    let view() = 
        let result = {
            new View<Events, HexConverterModel, HexConverterWindow>() with 
                member this.EventStreams = 
                    [
                        this.Control.Value.PreviewTextInput |> Observable.map(fun args -> ValidateHexFormat(args.Text, fun() -> args.Handled <- true))
                        this.Control.Value.TextChanged |> Observable.map(fun args -> UpdateValue this.Control.Value.Text)
                    ]

                member this.SetBindings model = 
                    Binding.OneWay 
                        <@ 
                            this.Control.Value.Text <- String.Format("{0:X}", model.Value)
                        @>
        }
        result.CancelButton <- result.Control.Cancel
        result.DefaultOKButton <- result.Control.OK
        result

    let controller() = 
        fun event (model : HexConverterModel) ->
            match event with
            | ValidateHexFormat(text, cancel) ->
                let isValid, _ = Int32.TryParse(text, NumberStyles.HexNumber, null)
                if not isValid then cancel()
            | UpdateValue text ->
                model.Value <- Int32.Parse(text, NumberStyles.HexNumber)

        |> Controller.Create

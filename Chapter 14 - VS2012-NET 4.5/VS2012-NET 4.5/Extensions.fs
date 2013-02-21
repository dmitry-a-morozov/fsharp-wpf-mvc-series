[<AutoOpen>]
module Mvc.Wpf.Sample.Extensions 

let isNull x = x = null 
let isNotNull x = x <> null

open System.Windows.Data
open System.Windows.Controls

type TextBox with
    member this.ShowErrorInTooltip() = 
        let binding = Binding("(Validation.Errors).CurrentItem.ErrorContent", RelativeSource = RelativeSource.Self)
        this.SetBinding(TextBox.ToolTipProperty, binding) |> ignore

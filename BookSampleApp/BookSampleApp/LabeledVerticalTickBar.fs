namespace MyNameSpace

open System.Windows.Controls.Primitives
open System.Windows.Media
open System.Globalization
open System.Windows
open System

type LabeledVerticalTickBar() = 
    inherit TickBar()

    override this.OnRender dc = 

        let step = this.ActualHeight / ((this.Maximum - this.Minimum) / this.TickFrequency)
        { this.Maximum .. -this.TickFrequency .. this.Minimum}
        |> Seq.iteri (fun index value ->
            let s, font = if value = 0.0 then "BREAK EVEN", "Verdana Bold" else string value, "Verdana"
            let formattedText = FormattedText(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Typeface font, 10.0, Brushes.Black)
            let origin = Point(this.ReservedSpace * 0.5, y = step * float index - formattedText.Height * 0.5)
            dc.DrawText(formattedText, origin) 
        )

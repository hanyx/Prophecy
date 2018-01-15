Imports System.Windows.Forms.DataVisualization.Charting

Public Class Main
    Private Property SampleLength As Integer
    Private Property Multiplier As Integer
    Private Property PredictionWindow As Integer
    Private Property Network As Prophecy.Network
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Me.Multiplier = 10
        Me.SampleLength = 7
        Me.PredictionWindow = 1

        Me.btnNew.PerformClick()
    End Sub
    Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
        '// Create Data and Charts
        Me.CreateChart("Sinus", Color.DarkGreen, Me.CreateData(Me.SampleLength, Function(x) Math.Sin(x)))
        Me.CreateChart("Prediction", Color.Red)
        Me.CreateNetwork("Sinus Prediction")
    End Sub
    Private Sub btnLearn_Click(sender As Object, e As EventArgs) Handles btnLearn.Click
        Call New Threading.Thread(Sub() Me.Network.Train(Me.GetDataOf("Sinus"), 0.005, 0.001)) With {.IsBackground = True}.Start()
    End Sub
    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        Me.Network.Abort()
    End Sub
    Private Sub NetworkEventLearning(Data As Double()())
        Me.ChangeGUI(False)
    End Sub
    Private Sub NetworkEventIteration(Epoch As Integer, Err As Double)

        Me.UpdateNetworkChart("Prediction", Me.PredictionWindow)
        Me.UpdateStatus(String.Format("Epoch {0} Error {1}", Epoch, Err.ToString("F8")))

        '// ugly but will do for now
        If (Err.ToString("F8").Equals("0,00000000")) Then
            Me.Network.Abort()
            Me.UpdateStatus("Idle...")
        End If
    End Sub
    Private Sub NetworkEventFinished(Weights As Double())
        Me.ChangeGUI(True)
    End Sub
    Private Sub CreateNetwork(Name As String)
        '// Remove old handlers is new one is made
        If (Me.Network IsNot Nothing) Then
            RemoveHandler Me.Network.NetworkLearning, AddressOf Me.NetworkEventLearning
            RemoveHandler Me.Network.NetworkFinished, AddressOf Me.NetworkEventFinished
            RemoveHandler Me.Network.NetworkIteration, AddressOf Me.NetworkEventIteration
        End If
        '// Create network
        Me.Network = New Prophecy.Network(Name, 1, 5, 1)
        AddHandler Me.Network.NetworkLearning, AddressOf Me.NetworkEventLearning
        AddHandler Me.Network.NetworkFinished, AddressOf Me.NetworkEventFinished
        AddHandler Me.Network.NetworkIteration, AddressOf Me.NetworkEventIteration
    End Sub
    Private Sub ChangeGUI(state As Boolean)
        If (Me.InvokeRequired) Then
            Me.Invoke(Sub() Me.ChangeGUI(state))
        Else
            Me.btnNew.Enabled = state
            Me.btnLearn.Enabled = state
            Me.btnStop.Enabled = Not state
        End If
    End Sub
    Private Sub UpdateStatus(message As String)
        If (Me.InvokeRequired) Then
            Me.Invoke(Sub() Me.UpdateStatus(message))
        Else
            Me.LabelStatus.Text = String.Format("Status: {0}", message)
        End If
    End Sub
    Private Sub UpdateNetworkChart(Name As String, Window As Integer, Optional Offset As Double = 0.0R)
        If (Me.InvokeRequired) Then
            Me.Invoke(Sub() Me.UpdateNetworkChart(Name, Window, Offset))
        Else
            Me.Chart.Series(Name).Points.Clear()
            For i As Integer = Me.SampleLength - 1 To (Me.SampleLength - 1) + Window
                Dim x As Double = i / Me.Multiplier
                Me.Chart.Series(Name).Points.AddXY(x, (Me.Network.Compute({x}).Last + Offset))
            Next
        End If
    End Sub
    Private Function GetDataOf(Name As String) As Double()()
        Dim buffer As New List(Of Double())
        If (Me.Chart.Series.IndexOf(Name) > -1) Then
            For Each point As DataPoint In Me.Chart.Series(Name).Points
                buffer.Add({point.XValue, point.YValues.First})
            Next
        End If
        Return buffer.ToArray
    End Function
    Private Function CreateData(Length As Integer, f As Func(Of Double, Double)) As Double()()
        Dim points As New List(Of Double())
        For i As Integer = 0 To Length - 1
            Dim x As Double = i / Me.Multiplier
            points.Add(New Double() {x, f.Invoke(i)})
        Next
        Return points.ToArray
    End Function
    Private Sub CreateChart(Name As String, Color As Color)
        Me.Chart.Series.Add(Name)
        Me.Chart.Series(Name).Color = Color
        Me.Chart.Series(Name).XValueType = ChartValueType.Double
        Me.Chart.Series(Name).ChartType = SeriesChartType.Spline
    End Sub
    Private Sub CreateChart(Name As String, Color As Color, points()() As Double, Optional Max As Integer = -1)
        If (points.Any) Then
            If (Max = -1) Then Max = points.GetLength(0) - 1
            Me.Chart.Series.Clear()
            Me.Chart.Series.Add(Name)
            Me.Chart.Series(Name).Color = Color
            Me.Chart.Series(Name).XValueType = ChartValueType.Double
            Me.Chart.Series(Name).ChartType = SeriesChartType.Spline
            For i As Integer = 0 To points.GetLength(0) - 1
                Dim x As Double = i / Me.Multiplier
                Me.Chart.Series(Name).Points.AddXY(x, points(i).Last)
                If (i >= Max) Then Exit For
            Next
        End If
    End Sub
End Class

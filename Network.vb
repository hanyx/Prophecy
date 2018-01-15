
<Serializable>
Public Class Network
    Public Property Learning As Boolean
    Public Property NetworkName As String
    Public Property NetworkInput As Integer
    Public Property NetworkHidden As Integer
    Public Property NetworkOutput As Integer
    Private Property Input As Double()
    Private Property Hidden As Double()
    Private Property Output As Double()
    Private Property OutputBias As Double()
    Private Property HiddenBias As Double()
    Private Property HiddenInputWeights As Double()()
    Private Property HiddenOutputWeights As Double()()
    <NonSerialized> Public Event NetworkLearning(Data()() As Double)
    <NonSerialized> Public Event NetworkFinished(Weights() As Double)
    <NonSerialized> Public Event NetworkIteration(Epoch As Integer, Err As Double)
    Sub New(Name As String, Input As Integer, Hidden As Integer, Output As Integer)
        Me.NetworkName = Name
        Me.NetworkInput = Input
        Me.NetworkHidden = Hidden
        Me.NetworkOutput = Output
        Me.Input = New Double(Input - 1) {}
        Me.Hidden = New Double(Hidden - 1) {}
        Me.Output = New Double(Output - 1) {}
        Me.HiddenInputWeights = Me.InitializeWeights(Input, Hidden, 0.0)
        Me.HiddenBias = New Double(Hidden - 1) {}
        Me.HiddenOutputWeights = Me.InitializeWeights(Hidden, Output, 0.0)
        Me.OutputBias = New Double(Output - 1) {}
        Me.RandomizeWeights()
    End Sub
    Public Sub Abort()
        Me.Learning = False
    End Sub
    Public Sub SetWeights(Weights As Double())
        Dim length As Integer = Me.WeightLength, index As Integer = 0
        If (Weights.Length <> length) Then Throw New Exception(String.Format("weight array length mismatch; {0} <> {1}", Weights.Length, length))
        For i As Integer = 0 To Me.NetworkInput - 1
            For j As Integer = 0 To Me.NetworkHidden - 1
                Me.HiddenInputWeights(i)(j) = Weights(index)
                index += 1
            Next j
        Next i
        For j As Integer = 0 To Me.NetworkHidden - 1
            Me.HiddenBias(j) = Weights(index)
            index += 1
        Next j
        For j As Integer = 0 To Me.NetworkHidden - 1
            For k As Integer = 0 To Me.NetworkOutput - 1
                Me.HiddenOutputWeights(j)(k) = Weights(index)
                index += 1
            Next k
        Next j
        For k As Integer = 0 To Me.NetworkOutput - 1
            Me.OutputBias(k) = Weights(k)
            k += 1
        Next k
    End Sub
    Public Function GetWeights() As Double()
        Dim length As Integer = Me.WeightLength, weights(length - 1) As Double, w As Integer = 0
        For i As Integer = 0 To Me.NetworkInput - 1
            For j As Integer = 0 To Me.NetworkHidden - 1
                weights(w) = Me.HiddenInputWeights(i)(j)
                w += 1
            Next j
        Next i
        For j As Integer = 0 To Me.NetworkHidden - 1
            weights(w) = Me.HiddenBias(j)
            w += 1
        Next j
        For j As Integer = 0 To Me.NetworkHidden - 1
            For k As Integer = 0 To Me.NetworkOutput - 1
                weights(w) = Me.HiddenOutputWeights(j)(k)
                w += 1
            Next k
        Next j
        For k As Integer = 0 To Me.NetworkOutput - 1
            weights(w) = Me.OutputBias(k)
            w += 1
        Next k
        Return weights
    End Function
    Public Function Compute(Values As Double()) As Double()
        Dim hiddenSums(Me.NetworkHidden - 1) As Double
        Dim outputSums(Me.NetworkOutput - 1) As Double
        Dim outputResult(Me.NetworkOutput - 1) As Double
        For i As Integer = 0 To Me.NetworkInput - 1
            Me.Input(i) = Values(i)
        Next i
        For j As Integer = 0 To Me.NetworkHidden - 1
            For i As Integer = 0 To Me.NetworkInput - 1
                hiddenSums(j) += Me.Input(i) * Me.HiddenInputWeights(i)(j)
            Next i
        Next j
        For j As Integer = 0 To Me.NetworkHidden - 1
            hiddenSums(j) += Me.HiddenBias(j)
        Next j
        For j As Integer = 0 To Me.NetworkHidden - 1
            Me.Hidden(j) = Me.Activation(hiddenSums(j))
        Next j
        For k As Integer = 0 To Me.NetworkOutput - 1
            For j As Integer = 0 To Me.NetworkHidden - 1
                outputSums(k) += Me.Hidden(j) * Me.HiddenOutputWeights(j)(k)
            Next j
        Next k
        For k As Integer = 0 To Me.NetworkOutput - 1
            outputSums(k) += Me.OutputBias(k)
        Next k
        Array.Copy(outputSums, Me.Output, Output.Length)
        Array.Copy(Me.Output, outputResult, outputResult.Length)
        Return outputResult
    End Function
    Public Sub Train(Data As Double()(), LearnRate As Double, Momentum As Double)
        Dim epoch As Integer = 0

        Dim hiddenOutputGradient()() As Double = Me.InitializeWeights(Me.NetworkHidden, Me.NetworkOutput, 0.0)
        Dim OutputBiasGradient(Me.NetworkOutput - 1) As Double

        Dim hiddenInputGradient()() As Double = Me.InitializeWeights(Me.NetworkInput, Me.NetworkHidden, 0.0)
        Dim hiddenBiasGradient(Me.NetworkHidden - 1) As Double

        Dim outputActivation(Me.NetworkOutput - 1) As Double
        Dim hiddenActivation(Me.NetworkHidden - 1) As Double


        Dim hiddenInputDelta()() As Double = Me.InitializeWeights(Me.NetworkInput, Me.NetworkHidden, 0.0)
        Dim hiddenBiasDelta(Me.NetworkHidden - 1) As Double
        Dim hiddenOutputDelta()() As Double = Me.InitializeWeights(Me.NetworkHidden, Me.NetworkOutput, 0.0)
        Dim outputBiasDelta(Me.NetworkOutput - 1) As Double


        Dim inputValues(Me.NetworkInput - 1) As Double
        Dim outputValues(Me.NetworkOutput - 1) As Double

        Dim sequence(Data.Length - 1) As Integer
        For i As Integer = 0 To sequence.Length - 1
            sequence(i) = i
        Next i
        Me.Learning = True

        RaiseEvent NetworkLearning(Data)
        Do
            epoch += 1

            If epoch Mod 100 = 0 Then
                RaiseEvent NetworkIteration(epoch, Me.GetError(Data))
            End If

            Me.Shuffle(sequence)
            For index As Integer = 0 To Data.Length - 1
                Dim idx As Integer = sequence(index)
                Array.Copy(Data(idx), inputValues, Me.NetworkInput)
                Array.Copy(Data(idx), Me.NetworkInput, outputValues, 0, Me.NetworkOutput)
                Me.Compute(inputValues)

                For k As Integer = 0 To Me.NetworkOutput - 1
                    Dim derivative As Double = 1.0
                    outputActivation(k) = (outputValues(k) - Me.Output(k)) * derivative
                Next k

                For j As Integer = 0 To Me.NetworkHidden - 1
                    For k As Integer = 0 To Me.NetworkOutput - 1
                        hiddenOutputGradient(j)(k) = outputActivation(k) * Me.Hidden(j)
                    Next k
                Next j

                For k As Integer = 0 To Me.NetworkOutput - 1
                    OutputBiasGradient(k) = outputActivation(k) * 1.0
                Next k

                For j As Integer = 0 To Me.NetworkHidden - 1
                    Dim sum As Double = 0.0
                    For k As Integer = 0 To Me.NetworkOutput - 1
                        sum += outputActivation(k) * Me.HiddenOutputWeights(j)(k)
                    Next k
                    Dim derivative As Double = (1 + Me.Hidden(j)) * (1 - Me.Hidden(j))
                    hiddenActivation(j) = sum * derivative
                Next j

                For i As Integer = 0 To Me.NetworkInput - 1
                    For j As Integer = 0 To Me.NetworkHidden - 1
                        hiddenInputGradient(i)(j) = hiddenActivation(j) * Me.Input(i)
                    Next j
                Next i

                For j As Integer = 0 To Me.NetworkHidden - 1
                    hiddenBiasGradient(j) = hiddenActivation(j) * 1.0
                Next j

                For i As Integer = 0 To Me.NetworkInput - 1
                    For j As Integer = 0 To Me.NetworkHidden - 1
                        Dim delta As Double = hiddenInputGradient(i)(j) * LearnRate
                        Me.HiddenInputWeights(i)(j) += delta
                        Me.HiddenInputWeights(i)(j) += hiddenInputDelta(i)(j) * Momentum
                        hiddenInputDelta(i)(j) = delta
                    Next j
                Next i

                For j As Integer = 0 To Me.NetworkHidden - 1
                    Dim delta As Double = hiddenBiasGradient(j) * LearnRate
                    Me.HiddenBias(j) += delta
                    Me.HiddenBias(j) += hiddenBiasDelta(j) * Momentum
                    hiddenBiasDelta(j) = delta
                Next j

                For j As Integer = 0 To Me.NetworkHidden - 1
                    For k As Integer = 0 To Me.NetworkOutput - 1
                        Dim delta As Double = hiddenOutputGradient(j)(k) * LearnRate
                        Me.HiddenOutputWeights(j)(k) += delta
                        Me.HiddenOutputWeights(j)(k) += hiddenOutputDelta(j)(k) * Momentum
                        hiddenOutputDelta(j)(k) = delta
                    Next k
                Next j

                For k As Integer = 0 To Me.NetworkOutput - 1
                    Dim delta As Double = OutputBiasGradient(k) * LearnRate
                    Me.OutputBias(k) += delta
                    Me.OutputBias(k) += outputBiasDelta(k) * Momentum
                    outputBiasDelta(k) = delta
                Next k
            Next index
        Loop While Me.Learning

        RaiseEvent NetworkFinished(Me.GetWeights())
    End Sub
    Private Sub Shuffle(sequence() As Integer)
        For i As Integer = 0 To sequence.Length - 1
            Dim value As Integer = Network.Randomizer.Next(i, sequence.Length)
            Dim previous As Integer = sequence(value)
            sequence(value) = sequence(i)
            sequence(i) = previous
        Next i
    End Sub
    Private Function GetError(data()() As Double) As Double
        Dim squared As Double = 0.0
        Dim inputValues(Me.NetworkInput - 1) As Double
        Dim outputValues(Me.NetworkOutput - 1) As Double
        For i As Integer = 0 To data.Length - 1
            Array.Copy(data(i), inputValues, Me.NetworkInput)
            Array.Copy(data(i), Me.NetworkInput, outputValues, 0, Me.NetworkOutput)
            Dim values() As Double = Me.Compute(inputValues)
            For j As Integer = 0 To Me.NetworkOutput - 1
                Dim err As Double = outputValues(j) - values(j)
                squared += err * err
            Next j
        Next i
        Return squared / data.Length
    End Function
    Private Sub RandomizeWeights()
        Dim Weight(Me.WeightLength - 1) As Double, lowest As Double = -0.001, highest As Double = +0.001
        For i As Integer = 0 To Weight.Length - 1
            Weight(i) = (highest - lowest) * Network.Randomizer.NextDouble + lowest
        Next i
        Me.SetWeights(Weight)
    End Sub
    Private Function InitializeWeights(Rows As Integer, Columns As Integer, Value As Double) As Double()()
        Dim result(Rows - 1)() As Double
        For r As Integer = 0 To result.Length - 1
            result(r) = New Double(Columns - 1) {}
        Next r
        For i As Integer = 0 To Rows - 1
            For j As Integer = 0 To Columns - 1
                result(i)(j) = Value
            Next j
        Next i
        Return result
    End Function
    Public ReadOnly Property Activation(x As Double) As Double
        Get
            If x < -20.0 Then
                Return -1.0
            ElseIf x > 20.0 Then
                Return 1.0
            Else
                Return Math.Tanh(x)
            End If
        End Get
    End Property
    Public ReadOnly Property WeightLength As Integer
        Get
            Return (Me.NetworkInput * Me.NetworkHidden) + (Me.NetworkHidden * Me.NetworkOutput) + Me.NetworkHidden + Me.NetworkOutput
        End Get
    End Property
    Public Shared ReadOnly Property Randomizer As Random
        Get
            Static rnd As New Random(Environment.TickCount)
            Return rnd
        End Get
    End Property
End Class

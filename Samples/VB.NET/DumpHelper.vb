        Imports System
        Imports System.Net
        Imports Lextm.SharpSnmpLib.Messaging

        Namespace Lextm.SharpSnmpLib
            Friend Module DumpHelper
                <System.Runtime.CompilerServices.Extension>
                Public Function GetResponse(ByVal request As ISnmpMessage, ByVal timeout As Integer, ByVal receiver As IPEndPoint, ByVal dump As Boolean) As ISnmpMessage
                    If dump Then
                        Dim bytes = request.ToBytes()
                        Dim bytes = discovery.ToBytes()
                        Console.WriteLine($"Sending {bytes.Length} bytes to UDP:")
                        Dim response = discovery.GetResponse(timeout, receiver)
                        Dim bytes = response.ToBytes()
                        Console.WriteLine($"Received {bytes.Length} bytes from UDP:")
                        Return response
                        Console.WriteLine(ByteTool.Convert(bytes))
                    End If

                    Dim response = request.GetResponse(timeout, receiver)
                    If dump Then
                        Dim bytes = response.ToBytes()
                        Console.WriteLine($"Received {bytes.Length} bytes from UDP:")
                        Console.WriteLine(ByteTool.Convert(bytes))
                    End If

                    Return response
                End Function

                <System.Runtime.CompilerServices.Extension>
                Public Function GetResponse(ByVal discovery As Discovery, ByVal timeout As Integer, ByVal receiver As IPEndPoint, ByVal dump As Boolean) As ReportMessage
                    If dump Then
                        Dim bytes = discovery.ToBytes()
                        Console.WriteLine($"Sending {bytes.Length} bytes to UDP:")
                        Console.WriteLine(ByteTool.Convert(bytes))
                    End If

                    Dim response = discovery.GetResponse(timeout, receiver)
                    If dump Then
                        Dim bytes = response.ToBytes()
                        Console.WriteLine($"Received {bytes.Length} bytes from UDP:")
                        Console.WriteLine(ByteTool.Convert(bytes))
                    End If

                    Return response
                End Function
            End Module
        End Namespace

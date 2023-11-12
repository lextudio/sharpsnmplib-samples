﻿'
' Created by SharpDevelop.
' User: Administrator
' Date: 2010/4/25
' Time: 14:16
'
' To change this template use Tools | Options | Coding | Edit Standard Headers.
'
Imports System.Net
Imports Lextm.SharpSnmpLib
Imports Samples.Pipeline
Imports Lextm.SharpSnmpLib.Security

Module Program
    Public Sub Main(ByVal args As String())
        If args.Length = 0 Then
            Dim idEngine = ByteTool.Convert("8000000001020304")
            Dim users As UserRegistry = New UserRegistry()
            users.Add(New OctetString("neither"), DefaultPrivacyProvider.DefaultPair)
            users.Add(New OctetString("authen"), New DefaultPrivacyProvider(New MD5AuthenticationProvider(New OctetString("authentication"))))
            If DESPrivacyProvider.IsSupported Then
                users.Add(New OctetString("privacy"), New DESPrivacyProvider(New OctetString("privacyphrase"), New MD5AuthenticationProvider(New OctetString("authentication"))))
            End If
            If AESPrivacyProviderBase.IsSupported Then
                users.Add(New OctetString("aes"), New AESPrivacyProvider(New OctetString("privacyphrase"), New MD5AuthenticationProvider(New OctetString("authentication"))))
                users.Add(New OctetString("aes192"), New AES192PrivacyProvider(New OctetString("privacyphrase"), New MD5AuthenticationProvider(New OctetString("authentication"))))
                users.Add(New OctetString("aes256"), New AES256PrivacyProvider(New OctetString("privacyphrase"), New MD5AuthenticationProvider(New OctetString("authentication"))))
            End If
            Dim trapv As TrapV1MessageHandler = New TrapV1MessageHandler()
            AddHandler trapv.MessageReceived, AddressOf Program.WatcherTrapV1Received
            Dim trapv1Mapping As HandlerMapping = New HandlerMapping("v1", "TRAPV1", trapv)
            Dim trapv2 As TrapV2MessageHandler = New TrapV2MessageHandler()
            AddHandler trapv2.MessageReceived, AddressOf Program.WatcherTrapV2Received
            Dim trapv2Mapping As HandlerMapping = New HandlerMapping("v2,v3", "TRAPV2", trapv2)
            Dim inform As InformRequestMessageHandler = New InformRequestMessageHandler()
            AddHandler inform.MessageReceived, AddressOf Program.WatcherInformRequestReceived
            Dim informMapping As HandlerMapping = New HandlerMapping("v2,v3", "INFORM", inform)
            Dim store As ObjectStore = New ObjectStore()
            Dim v As Version1MembershipProvider = New Version1MembershipProvider(New OctetString("public"), New OctetString("public"))
            Dim v2 As Version2MembershipProvider = New Version2MembershipProvider(New OctetString("public"), New OctetString("public"))
            Dim v3 As Version3MembershipProvider = New Version3MembershipProvider()
            Dim membership As ComposedMembershipProvider = New ComposedMembershipProvider(New IMembershipProvider() {v, v2, v3})
            Dim handlerFactory As MessageHandlerFactory = New MessageHandlerFactory(New HandlerMapping() {trapv1Mapping, trapv2Mapping, informMapping})
            Dim pipelineFactory As SnmpApplicationFactory = New SnmpApplicationFactory(store, membership, handlerFactory)
            Using engine As SnmpEngine = New SnmpEngine(pipelineFactory, New Listener() With {.Users = users}, New EngineGroup(idEngine))
                engine.Listener.AddBinding(New IPEndPoint(IPAddress.Any, 162))
                engine.Start()
                Console.WriteLine("#SNMP is available at https://sharpsnmp.com")
                Console.WriteLine("Press any key to stop . . . ")
                Console.Read()
                engine.[Stop]()
            End Using
        End If
    End Sub

    Private Sub WatcherInformRequestReceived(ByVal sender As Object, ByVal e As InformRequestMessageReceivedEventArgs)
        Console.WriteLine(e.InformRequestMessage)
    End Sub

    Private Sub WatcherTrapV2Received(ByVal sender As Object, ByVal e As TrapV2MessageReceivedEventArgs)
        Console.WriteLine(e.TrapV2Message)
    End Sub

    Private Sub WatcherTrapV1Received(ByVal sender As Object, ByVal e As TrapV1MessageReceivedEventArgs)
        Console.WriteLine(e.TrapV1Message)
    End Sub
End Module

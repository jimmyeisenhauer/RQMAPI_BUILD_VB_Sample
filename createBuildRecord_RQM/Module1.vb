




Imports System.Xml
Imports System.IO
Imports System.Globalization
Imports System.Net
Imports System.Net.Security
Imports System.Text
Imports System.Text.RegularExpressions

Module Module1

    Sub Main()

        Dim project As String
        Dim buildDef As String
        Dim dt As DateTime = DateTime.Now
        Dim dtz As DateTime = DateTime.Now.ToUniversalTime()
        Dim builddate As String
        Dim builddatexml As String
        Dim cleanup As String = Nothing
        Dim JAZZ_HOST As String
        Dim JAZZ_HOST2 As String
        Dim username As String
        Dim password As String

        ' convert these to command line vars 
        project = "[enter RQM Project Name]"
        buildDef = "[Enter Build Definition Number]"
        cleanup = "no"
        JAZZ_HOST = "[enter server example rqm.server.com]"

        username = "[RQM Username]"
        password = "[RQM Password]"
        ' # in password won't work....defect too lazy to fix...
        '-------------------------

        JAZZ_HOST2 = JAZZ_HOST & ":9443"   'change if your port is different 

        Dim BUILDDEFURL As String = "https://" & JAZZ_HOST2 & "/jazz/service/com.ibm.rqm.integration.service.IIntegrationService/resources/" & project & "/builddefinition/urn:com.ibm.rqm:builddefinition:" & buildDef
        Dim JAZZ_BASE As String = "https://" & JAZZ_HOST2 & "/jazz/"
        Dim JAZZ_AUTH As String = "https://" & JAZZ_HOST2 & "/jazz/j_security_check?j_username={0}&j_password={1}"
        Dim BUILDRECORDURLADD As String = "https://" & JAZZ_HOST2 & "/jazz/service/com.ibm.rqm.integration.service.IIntegrationService/resources/" & project & "/buildrecord/"






        ' ----- checking for clean up flag
        If String.IsNullOrEmpty(cleanup) Then
            cleanup = "yes"
        End If
        '-----------------------------

        ' create new build record in in progress state

        builddate = dt.ToString("HHmm_ss_dd_MMM_yyyy", DateTimeFormatInfo.InvariantInfo)
        builddatexml = dtz.ToString("yyyy-MM-ddTHH:mm:ss", DateTimeFormatInfo.InvariantInfo) & "Z"
        Dim buildname As String = builddate & "_" & project & "_" & buildDef
        Dim buildFile As String = Directory.GetCurrentDirectory() & "\" & buildname & ".xml"

        Dim buildwriter As New XmlTextWriter(buildFile, New UTF8Encoding(False))



        '  myXmlFileStreamHandle.Position = 0


        buildwriter.WriteStartDocument(True)
        buildwriter.WriteStartElement("ns2:buildrecord")
        buildwriter.WriteAttributeString("xmlns", "ns11", Nothing, "http://jazz.net/xmlns/alm/qm/v0.1/executionworkitem/v0.1")

        buildwriter.WriteAttributeString("xmlns", "ns8", Nothing, "http://jazz.net/xmlns/alm/qm/v0.1/testscript/v0.1/")

        buildwriter.WriteAttributeString("xmlns", "ns10", Nothing, "http://jazz.net/xmlns/alm/qm/qmadapter/task/v0.1")

        buildwriter.WriteAttributeString("xmlns", "ns7", Nothing, "http://jazz.net/xmlns/alm/qm/v0.1/executionresult/v0.1")

        buildwriter.WriteAttributeString("xmlns", "ns6", Nothing, "http://jazz.net/xmlns/alm/qm/v0.1/tsl/v0.1/")

        buildwriter.WriteAttributeString("xmlns", "ns5", Nothing, "http://jazz.net/xmlns/alm/v0.1/")

        buildwriter.WriteAttributeString("xmlns", "ns9", Nothing, "http://jazz.net/xmlns/alm/qm/qmadapter/v0.1")

        buildwriter.WriteAttributeString("xmlns", "ns3", Nothing, "http://schema.ibm.com/vega/2008/")

        buildwriter.WriteAttributeString("xmlns", "ns4", Nothing, "http://jazz.net/xmlns/alm/qm/v0.1/catalog/v0.1")

        buildwriter.WriteAttributeString("xmlns", "ns2", Nothing, "http://jazz.net/xmlns/alm/qm/v0.1/")

        buildwriter.WriteAttributeString("xmlns", Nothing, "http://purl.org/dc/elements/1.1/")

        buildwriter.WriteStartElement("title")
        buildwriter.WriteString(buildname)
        buildwriter.WriteEndElement()

        buildwriter.WriteStartElement("ns5:updated")
        buildwriter.WriteString(builddatexml)
        buildwriter.WriteEndElement()

        buildwriter.WriteStartElement("ns5:state")
        buildwriter.WriteString("com.ibm.rqm.buildintegration.buildstate.running")
        buildwriter.WriteEndElement()

        buildwriter.WriteStartElement("ns2:endtime")
        buildwriter.WriteString(builddatexml)
        buildwriter.WriteEndElement()

        buildwriter.WriteStartElement("ns2:status")
        buildwriter.WriteString("com.ibm.rqm.buildintegration.buildstatus.inprogress")
        buildwriter.WriteEndElement()

        buildwriter.WriteStartElement("ns2:providerTypeId")
        buildwriter.WriteString("com.ibm.rqm.buildintegration.common.manualProvider")
        buildwriter.WriteEndElement()


        buildwriter.WriteEndElement()
        buildwriter.WriteEndDocument()
        buildwriter.Flush()
        buildwriter.Close()

        Console.WriteLine("RQMBUILD: Created Build Record XML File.")

        ' end of create build xml file



        ' start of get build definition xml file

        ' deal with bad ssl cert
        ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)
        Dim request As HttpWebRequest = TryCast(WebRequest.Create(New Uri(JAZZ_BASE)), HttpWebRequest)
        Dim intialResponse As HttpWebResponse = TryCast(request.GetResponse(), HttpWebResponse)

        Dim container As New CookieContainer()

        For Each cookietext As String In intialResponse.Headers("Set-Cookie").Split(";")

            Dim trimmedCookie As String = cookietext.Trim()

            If trimmedCookie.StartsWith("Path") Then
                Continue For
            End If


            ' some reason cookie was Secure, and it didn't like the comma so i remove the entire thing.
            If trimmedCookie.StartsWith("Secure") Then
                trimmedCookie = trimmedCookie.Remove(0, 7)

            End If

            Dim c As New Cookie(trimmedCookie.Split("=")(0), trimmedCookie.Split("=")(1))
            c.Domain = JAZZ_HOST
            container.Add(c)



        Next
        Console.WriteLine("RQMBUILD: Created RQM Server Session.")

        'login
        request = TryCast(WebRequest.Create(New Uri([String].Format(JAZZ_AUTH, username, password))), HttpWebRequest)
        request.Method = "POST"
        request.ContentType = "application/x-www-form-urlencoded"
        request.Referer = JAZZ_BASE

        request.CookieContainer = container

        Dim response As HttpWebResponse = TryCast(request.GetResponse(), HttpWebResponse)

        If response.Headers("X-com-ibm-team-repository-web-auth-msg") IsNot Nothing AndAlso response.Headers("X-com-ibm-team-repository-web-auth-msg") = "authfailed" Then
            Throw New System.Security.Authentication.AuthenticationException("Authentication Error")
        End If


        response.Close()

        Console.WriteLine("RQMBUILD: Logged into RQM Server.")

        'get Build Definition xml
        request = TryCast(WebRequest.Create(New Uri([String].Format(BUILDDEFURL))), HttpWebRequest)

        request.Referer = response.ResponseUri.ToString()

        request.CookieContainer = container

        response = TryCast(request.GetResponse(), HttpWebResponse)

        Directory.GetCurrentDirectory()

        Dim buildDeffile As String = Directory.GetCurrentDirectory() + "\" & project & "_" & buildDef & "_buildDef.xml"

        Dim writer As New StreamWriter(buildDeffile)

        Dim reader As New StreamReader(response.GetResponseStream())
        Dim response_content As String = reader.ReadToEnd()

        writer.Write(response_content)
        writer.Close()
        reader.Close()
        response.Close()
        Console.WriteLine("RQMBUILD: Retrieved Build Definition File with a build ID of: " & buildDef & " from Project: " & project)

        ' end of get build definition xml file

        ' start intial create put build record

        request = TryCast(WebRequest.Create(New Uri([String].Format(BUILDRECORDURLADD))), HttpWebRequest)
        request.Method = "POST"
        request.ContentType = "text/xml"
        request.Referer = response.ResponseUri.ToString()
        request.CookieContainer = container

        Dim requestStream As Stream = Nothing
        Dim fileStream As FileStream = Nothing
        requestStream = request.GetRequestStream()
        fileStream = File.Open(buildFile, FileMode.Open)

        Dim buffer(1024) As Byte
        Dim bytesRead As Integer
        While True
            bytesRead = fileStream.Read(buffer, 0, buffer.Length)
            If bytesRead = 0 Then
                Exit While
            End If
            requestStream.Write(buffer, 0, bytesRead)
        End While

        ' The request stream must be closed before getting the response.
        requestStream.Close()
        fileStream.Close()

        response = TryCast(request.GetResponse(), HttpWebResponse)
        Dim BuildRecordID As String = response.GetResponseHeader("Content-Location")

        Console.WriteLine("RQMBUILD: Attempted creation of Build Record with a status code of: " & response.StatusCode)
        Console.WriteLine("RQMBUILD: New Build Record ID is: " & BuildRecordID)
        'Console.WriteLine("DEMO: View the Build Record in RQM.  Hit Enter when done")
        'Console.ReadLine()

        ' end of put build record

        ' begin add build record to build definition

        Dim doc As New Xml.XmlDocument

        doc.PreserveWhitespace = True
        doc.Load(buildDeffile)



        'Dim nodeList As XmlNodeList = doc.SelectNodes("//buildrecord")
        ' Finds the buildrecord node.
        Dim node As XmlNode = doc.LastChild
        ' Sets the pointer to the last record
        Dim element As XmlElement = doc.CreateElement("ns2", "buildrecord", node.NamespaceURI)
        ' Construct the new element using the namespace of the node.
        element.SetAttribute("href", BUILDRECORDURLADD & BuildRecordID)
        ' Add the href attribute to it (you can create some logic here to change this to whatever it needs to be obviously)
        Dim newNode As XmlNode = Nothing
        newNode = element

        node.AppendChild(newNode)
        ' Append the new constructed node to the document.


        Dim regex As New Regex(">\s*<")
        doc.InnerXml = regex.Replace(doc.InnerXml, "><")


        Dim output As New XmlTextWriter(buildDeffile, New UTF8Encoding(False))

        doc.WriteTo(Output)
        output.Close()





        Console.WriteLine("RQMBUILD: Added new build record to build def file.")

        ' end add build record to build definition

        'begin put new build definition record to RQM Server

        request = TryCast(WebRequest.Create(New Uri([String].Format(BUILDDEFURL))), HttpWebRequest)
        request.Method = "PUT"
        request.ContentType = "text/xml"
        request.Referer = response.ResponseUri.ToString()
        request.CookieContainer = container


        requestStream = request.GetRequestStream()
        fileStream = File.Open(buildDeffile, FileMode.Open)


        While True
            bytesRead = fileStream.Read(buffer, 0, buffer.Length)
            If bytesRead = 0 Then
                Exit While
            End If
            requestStream.Write(buffer, 0, bytesRead)
        End While

        ' The request stream must be closed before getting the response.
        requestStream.Close()
        fileStream.Close()


        response = TryCast(request.GetResponse(), HttpWebResponse)

        Console.WriteLine("RQMBUILD: Attempted update of Build Def Record with a status code of: " & response.StatusCode)

        ' Console.WriteLine("DEMO: View the Build Def Record Record in RQM with the Build Record attached.  Hit Enter when done")
        'Console.ReadLine()




        'end put new build def record


        ' begin change build record to OK


        'getting xml

        Console.WriteLine("RQMBUILD: Getting Build Record xml.")

        Dim BUILDRECORDUPDATEURL As String = "https://" & JAZZ_HOST2 & "/jazz/service/com.ibm.rqm.integration.service.IIntegrationService/resources/" & project & "/buildrecord/" & BuildRecordID

        request.Method = "GET"
        request.ContentType = "application/x-www-form-urlencoded"
        request = TryCast(WebRequest.Create(New Uri([String].Format(BUILDRECORDUPDATEURL))), HttpWebRequest)

        request.Referer = response.ResponseUri.ToString()

        request.CookieContainer = container

        response = TryCast(request.GetResponse(), HttpWebResponse)

        Directory.GetCurrentDirectory()

        Dim buildRecordUpdate As String = Directory.GetCurrentDirectory() + "\" & project & "_" & BuildRecordID & "_buildRecord.xml"

        Dim writerupdate As New StreamWriter(buildRecordUpdate)

        Dim readerupdate As New StreamReader(response.GetResponseStream())
        Dim response_contentupdate As String = readerupdate.ReadToEnd()

        writerupdate.Write(response_contentupdate)
        writerupdate.Close()
        readerupdate.Close()
        response.Close()

        ' getting xml done
        Console.WriteLine("RQMBUILD: Build record retrieved.  Modifying it now...")
       
        'modify xml

        Dim docU As New Xml.XmlDocument
        Dim nodeU As XmlNode
        'Dim nodeU2 As XmlNode
        docU.PreserveWhitespace = True
        docU.Load(buildRecordUpdate)
        nodeU = docU.DocumentElement



        For Each nodeU In nodeU.ChildNodes

            If nodeU.Name = "ns5:state" Then

                nodeU.InnerText = "com.ibm.rqm.buildintegration.buildstate.complete"
            End If

            If nodeU.Name = "ns2:status" Then
                nodeU.InnerText = "com.ibm.rqm.buildintegration.buildstatus.ok"
            End If

        Next

        Dim regexU As New Regex(">\s*<")
        docU.InnerXml = regex.Replace(docU.InnerXml, "><")


        Dim outputU As New XmlTextWriter(buildRecordUpdate, New UTF8Encoding(False))

        docU.WriteTo(outputU)
        outputU.Close()

        Console.WriteLine("RQMBUILD: Done modifying Build Record...need to update it")
        'end change xml


        'begin put changes


        request = TryCast(WebRequest.Create(New Uri([String].Format(BUILDRECORDUPDATEURL))), HttpWebRequest)
        request.Method = "PUT"
        request.ContentType = "text/xml"
        request.Referer = response.ResponseUri.ToString()
        request.CookieContainer = container


        requestStream = request.GetRequestStream()
        fileStream = File.Open(buildRecordUpdate, FileMode.Open)


        While True
            bytesRead = fileStream.Read(buffer, 0, buffer.Length)
            If bytesRead = 0 Then
                Exit While
            End If
            requestStream.Write(buffer, 0, bytesRead)
        End While

        ' The request stream must be closed before getting the response.
        requestStream.Close()
        fileStream.Close()


        response = TryCast(request.GetResponse(), HttpWebResponse)

        Console.WriteLine("RQMBUILD: Attempted update of Build Record to OK with a status code of: " & response.StatusCode)




        ' end change build record to OK




        ' cleanup xml files
        ' setting cleanup to no leaves xml files for debug
        If cleanup = "yes" Then
            Console.WriteLine("RQMBUILD: Cleaning up xml files")

            If System.IO.File.Exists(buildFile) = True Then
                System.IO.File.Delete(buildFile)
            End If

            If System.IO.File.Exists(buildDeffile) = True Then
                System.IO.File.Delete(buildDeffile)
            End If

            If System.IO.File.Exists(buildRecordUpdate) = True Then
                System.IO.File.Delete(buildRecordUpdate)
            End If

        End If


        ' added to stop program exit
        Console.WriteLine("RQMBUILD: Complete hit enter to exit")
        Console.ReadLine()

    End Sub

End Module





Imports System.Security.Cryptography.X509Certificates

Module badsslFix
    Function CertificateValidationCallBack( _
   ByVal sender As Object, _
   ByVal certificate As X509Certificate, _
   ByVal chain As X509Chain, _
   ByVal sslPolicyErrors As Net.Security.SslPolicyErrors _
) As Boolean

        Return True
    End Function
End Module

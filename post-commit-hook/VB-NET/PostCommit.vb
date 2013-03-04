Imports System.Text
Imports System.IO
Imports System.Net.Mail

Module PostCommit

    Private SVNPath As String = Environment.GetEnvironmentVariable("VISUALSVN_SERVER")
    Function Main(ByVal Args() As String) As Integer

        'Check if revision number and revision path have been supplied.
        If Args.Length < 2 Then
            Console.Error.WriteLine("Invalid arguments sent - <REPOSITORY> <REV> required")
            Return 1
        End If

        'Check if VisualSVN is installed.
        If String.IsNullOrEmpty(SVNPath) Then
            Console.Error.WriteLine("VISUALSVN_SERVER environment variable does not exist. VisualSVN installed?")
            Return 1
        End If

        'Get the required information using SVNLook.
        Dim Author As String = SVNLook("author", Args)
        Dim Message As String = SVNLook("log", Args)
        Dim Changed As String = SVNLook("changed", Args)

        'Get the branch from the first change in the list.
        Dim ChangeList() As String = Changed.Split(Environment.NewLine)
        Dim ChangeFirst As String = ChangeList(0).Remove(0, 4)
        Dim ChangeFirstSlash As Integer = ChangeFirst.IndexOf("/")
        Dim RepoBranch As String = ChangeFirst.Substring(0, ChangeFirstSlash)

        'Get the name of the repository from the first argument, which is the repo path.
        Dim RepoName As String = Args(0).ToString.Substring(Args(0).LastIndexOf("\") + 1)

        'Get the email template and fill it in. This template can be anywhere, and can be a .HTML file
        'for more control over the structure.
        Dim EmailTemplatePath As String = "c:\hooks\PostCommit.txt"
        Dim EmailTemplate = String.Format(File.ReadAllText(EmailTemplatePath), Author, Message, Changed)

        'Construct the email that will be sent. You can use the .IsBodyHtml property if you are
        'using an HTML template.
        Dim Subject As String = String.Format("commit number {0} for {1}", Args(1), RepoName)
        Dim MM As New MailMessage("<from email>", "<to email>")
        With MM
            .Body = EmailTemplate
            .Subject = Subject
        End With

        'Define your mail client. I am using Gmail here as the SMTP server, but you could
        'use IIS or Amazon SES or whatever you want.
        Dim MailClient As New SmtpClient("smtp.gmail.com")
        With MailClient
            .Port = 587
            .Credentials = New System.Net.NetworkCredential("<gmail username>", "<gmail password>")
            .EnableSsl = True
        End With

        MailClient.Send(MM)

        Return 0
    End Function

    '<summary>
    'Runs a command on svnlook.exe to get information
    'about a particular repo and revision.
    '</summary>
    '<param name="command">The svnlook command e.g. log, author, message.</param>
    '<param name="args">The arguments passed in to this exe (repo name and rev number).</param>
    '<returns>The output of svnlook.exe</returns>
    Private Function SVNLook(ByVal Command As String, ByVal Args() As String)
        Dim Output As New StringBuilder()
        Dim procMessage As New Process()

        'Start svnlook.exe in a process and pass it the required command-line args.
        With procMessage
            .StartInfo = New System.Diagnostics.ProcessStartInfo(SVNPath & "bin\svnlook.exe", String.Format("{0} ""{1}"" -r ""{2}""", Command, Args(0), Args(1)))
            With .StartInfo
                .RedirectStandardOutput = True
                .UseShellExecute = False
            End With
        End With
        procMessage.Start()

        'While reading the output of svnlook, append it to the stringbuilder then
        'return the output.
        While Not procMessage.HasExited
            Output.Append(procMessage.StandardOutput.ReadToEnd())
        End While

        Return Output.ToString()
    End Function

End Module
